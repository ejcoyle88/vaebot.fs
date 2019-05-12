module Bot

open Utility
open Configuration
open BotCommands

open System

let mapToState (res: Result<(BotConfiguration * IrcConnection), string>) =
    match res with
    | Error x -> Error x
    | Ok x -> Ok({ config = (fst x); connection = (snd x) })

let initialSetup (r: Result<BotState, string>) =
    match r with
    | Error x -> Error x
    | Ok state ->
        write state (sprintf "PASS %s" state.config.password)
        write state (sprintf "NICK %s" state.config.username)
        write state "JOIN #vaeix"
        Ok state

let apply (fList: ('a->'b) list) (x: 'a) = [ for f in fList do yield f x ]

let rec railApply (l: ('a -> Result<'b,string>) list) (i: 'a) =
    match l with
    | [] -> Ok Unchecked.defaultof<'b>
    | [x] -> (x i)
    | x::xs ->
        match x i with
        | Error s -> Error s
        | Ok _ -> railApply xs i

let rec handleInput (state: BotState) (halt: bool) =
    if halt || state.connection.Reader.EndOfStream then Ok state
    else
        let line = state.connection.Reader.ReadLine()
        Console.WriteLine (sprintf "<- %s" line)
        let handlers = apply inputHandlers state
        match railApply handlers line with
        | Error x ->
            Console.WriteLine x
            handleInput state true
        | Ok _ -> handleInput state false

let inputLoop (r: Result<BotState, string>) =
    match r with
    | Error x -> Error x
    | Ok state -> handleInput state false

let shutdown (r: Result<BotState, string>) =
    match r with
    | Error x -> Console.WriteLine x
    | Ok _ -> Console.WriteLine "Bot shutdown..."

let run =
    getConfiguration
    |> IrcConnection.ConnectWithConfig
    |> mapToState
    |> initialSetup
    |> inputLoop
    |> shutdown