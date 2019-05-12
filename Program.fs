// Learn more about F# at http://fsharp.org

open System
open QueuedBot

[<EntryPoint>]
let main argv =
    Console.WriteLine "Getting the motors running..."
    Bot.Init
    0 // return an integer exit code
