module QueuedBot

open Utility
open Configuration

open System
open System.IO
open System.Text.RegularExpressions
open System.Threading

type OutputAgent () =
    static let sendMessage (writer: StreamWriter) msg =
        Console.WriteLine (sprintf "-> %s" msg)
        writer.WriteLine msg

    static let CreateAgent writer = MailboxProcessor.Start(fun inbox ->
        let rec messageLoop () = async {
            let! msg = inbox.Receive()

            sendMessage writer msg

            return! messageLoop ()
        }

        messageLoop ()
    )

    static member CreateWriter writer =
        let agent = CreateAgent writer
        agent.Post

type CommandState = { botState: BotState; send: string -> unit }

let [<Literal>] CmdPrefix = "|"

let (|Prefix|_|)(p: string) (s: string) = 
    if s.StartsWith(p) 
        then Some(s.Substring(p.Length))
        else None

let (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        if m.Success 
            then Some(List.tail [ for g in m.Groups -> g.Value ])
            else None

let selfPrivMsgPattern = ":vaeix!vaeix@vaeix.tmi.twitch.tv PRIVMSG (#.+) :(.+)"
let privMsgPattern = ":.+!.+@(.+).tmi.twitch.tv PRIVMSG (#.+) :(.+)"

let pingHander cmdState line =
    if line = "PING :tmi.twitch.tv" then
         cmdState.send "PONG :tmi.twitch.tv"
    Ok ()

let isaacyWHandler cmdState line =
    match line with
    | Regex selfPrivMsgPattern groups ->
        if groups.[1] = (CmdPrefix + "isaacyW") then
            cmdState.send (sprintf "PRIVMSG %s :isaacy1 isaacy2" groups.[0])
            Thread.Sleep 600
            cmdState.send (sprintf "PRIVMSG %s :isaacy3 isaacy4" groups.[0])
    | _ -> ()
    Ok ()

let handleJoin cmdState line =
    let cmd = CmdPrefix + "join"
    match line with
    | Regex selfPrivMsgPattern groups ->
        match groups.[1] with
        | Prefix cmd msg ->
            let msgParts = msg.Trim().Split(' ')
            if msgParts.Length >= 1 then
                cmdState.send (sprintf "JOIN #%s" msgParts.[0])
        | _ -> ()
    | _ -> ()
    Ok()

let handleLeave cmdState line =
    let cmd = CmdPrefix + "leave"
    match line with
    | Regex selfPrivMsgPattern groups ->
        match groups.[1] with
        | Prefix cmd _ ->
            cmdState.send (sprintf "LEAVE #%s" groups.[0])
        | _ -> ()
    | _ -> ()
    Ok()

let inputHandlers = [pingHander; isaacyWHandler; handleJoin; handleLeave]

type InputAgent () =
    static let processMessage cmdState line = 
        Console.WriteLine (sprintf "<- %s" line)
        let handlers = apply inputHandlers cmdState
        match railApply handlers line with
        | Error x ->
            Console.WriteLine x
            false
        | _ -> true

    static let CreateAgent cmdState = MailboxProcessor.Start(fun inbox ->
        let rec messageLoop () = async {
            let! msg = inbox.Receive()
            let shouldContinue = processMessage cmdState msg
            if not shouldContinue then return ()
            else return! messageLoop ()
        }

        messageLoop ()
    )

    static member CreateReader cmdState =
        let agent = CreateAgent cmdState
        agent.Post


type Bot (state: BotState) =
    member this.Run =
        let writer = OutputAgent.CreateWriter state.connection.Writer
        let cmdState = {botState = state; send = writer}
        let reader = InputAgent.CreateReader cmdState

        writer (sprintf "PASS %s" state.config.password)
        writer (sprintf "NICK %s" state.config.username)
        writer "JOIN #vaeix"
        
        while not state.connection.Reader.EndOfStream do
            let line = state.connection.Reader.ReadLine()
            reader line
    
    static member Init =
        let stateRes = getConfiguration
                    |> IrcConnection.ConnectWithConfig
                    |> mapToState
        match stateRes with
        | Error x -> Console.WriteLine x
        | Ok state -> Bot(state).Run