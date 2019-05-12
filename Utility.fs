module Utility

open System
open System.IO
open System.Threading
open System.Net.Sockets
open FSharp.Control.Tasks.V2

type BotConfiguration = {username: string; password: string; host: string; port: int}

type IrcConnection(tcpClient: TcpClient, reader: StreamReader, writer: StreamWriter) =
    member __.Writer = writer
    member __.Reader = reader
    member __.TcpClient = tcpClient
    static member ConnectTo (host: string) (port: int) =
        let tcpClient = new TcpClient()
        let connectionResult = 
            try
                Console.WriteLine (sprintf "Attempting to connect to %s:%d" host port)
                tcpClient.Connect(host, port)
                Ok ()
            with
            | :? SocketException -> Error "SocketException"
            | :? ObjectDisposedException -> Error "ObjectDisposedException"

        match connectionResult with
        | Error x -> Error x
        | Ok _ -> 
            let inputStream = new StreamReader(tcpClient.GetStream())
            let outputStream = new StreamWriter(tcpClient.GetStream())
            outputStream.AutoFlush <- true
            let result = IrcConnection(tcpClient, inputStream, outputStream)
            Ok result
    static member ConnectWithConfig (config: Result<BotConfiguration, string>) =
        match config with
        | Error x -> Error x
        | Ok cfg -> 
            let connection = IrcConnection.ConnectTo cfg.host cfg.port
            match connection with
            | Error x -> Error x
            | Ok conn -> Ok((cfg, conn))

type BotState = { config: BotConfiguration; connection: IrcConnection }

let write (state: BotState) (msg: string) =
    Console.WriteLine (sprintf "-> %s" msg)
    state.connection.Writer.WriteLine msg

let writeAsync (state: BotState) (msg: string) =
    task {
        Console.WriteLine (sprintf "-> %s" msg)
        do! state.connection.Writer.WriteLineAsync msg
    }

let writeMultipleAsync (delay: int) (state: BotState) (msgs: string list) =
    task {
        for msg in msgs do
            do! writeAsync state msg
            Thread.Sleep delay
    }

let apply (fList: ('a->'b) list) (x: 'a) = [ for f in fList do yield f x ]

let rec railApply (l: ('a -> Result<'b,string>) list) (i: 'a) =
    match l with
    | [] -> Ok Unchecked.defaultof<'b>
    | [x] -> (x i)
    | x::xs ->
        match x i with
        | Error s -> Error s
        | Ok _ -> railApply xs i

let mapToState (res: Result<(BotConfiguration * IrcConnection), string>) =
    match res with
    | Error x -> Error x
    | Ok x -> Ok({ config = (fst x); connection = (snd x) })