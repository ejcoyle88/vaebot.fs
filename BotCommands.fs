module BotCommands

open Utility

open System.Text.RegularExpressions

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

let handlePing (state: BotState) (line: string) =
    if line = "PING :tmi.twitch.tv" then
        write state "PONG :tmi.twitch.tv"
    Ok ()

let handleTest (state: BotState) (line: string) =
    match line with
    | Regex selfPrivMsgPattern groups ->
        if groups.[1] = "!test" then
            writeMultipleAsync 1000 state [
                (sprintf "PRIVMSG %s :test" groups.[0]);
                (sprintf "PRIVMSG %s :test 2" groups.[0])
            ] |> ignore
            Ok ()
        else Ok ()
    | _ -> Ok ()

let handleJoin (state: BotState) (line: string) =
    match line with
    | Regex selfPrivMsgPattern groups ->
        match groups.[1] with
        | Prefix "!join" msg ->
            let msgParts = msg.Trim().Split(' ')
            if msgParts.Length >= 1 then
                write state (sprintf "JOIN #%s" msgParts.[0])
            Ok ()
        | _ -> Ok ()
    | _ -> Ok ()

let handleLeave (state: BotState) (line: string) =
    match line with
    | Regex selfPrivMsgPattern groups ->
        match groups.[1] with
        | Prefix "!leave" _ ->
            write state (sprintf "LEAVE #%s" groups.[0])
            Ok ()
        | _ -> Ok ()
    | _ -> Ok ()

let handleQuit (state: BotState) (line: string) =
    match line with
    | Regex privMsgPattern groups ->
        if groups.[2] = "!quit" && groups.[0] = "vaeix" then
            write state (sprintf "PRIVMSG %s :Bye! BibleThump" groups.[1])
            Error "Shutting down..."
        else Ok ()
    | _ -> Ok ()

let inputHandlers = [handlePing; handleTest; handleQuit; handleJoin]