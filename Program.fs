// Learn more about F# at http://fsharp.org

open System
open Bot

[<EntryPoint>]
let main argv =
    Console.WriteLine "Getting the motors running..."
    run
    0 // return an integer exit code
