module Configuration

open System
open Utility

let envVars (varName: String) = 
    let envVar = 
        Environment.GetEnvironmentVariables()
        |> Seq.cast<System.Collections.DictionaryEntry>
        |> Seq.map (fun d -> d.Key :?> string, d.Value :?> string)
        |> Seq.tryFind (fun d -> (fst d) = varName)
    match envVar with
    | None -> Error (sprintf "Environment Variable not found: %s" varName)
    | Some x -> Ok(snd x)

let getEnvVarsDict1 (varName: string) =
    match envVars varName with
    | Error x -> Error x
    | Ok n -> 
        let m = [(varName, n);] |> Map.ofList
        Ok m

let getEnvVarsDict2 (varName: string) (pRes: Result<Map<string, string>, string>)=
    match pRes with
    | Error x -> Error x
    | Ok p ->
        match envVars varName with
        | Error x -> Error x
        | Ok n -> Ok (p.Add (varName, n))

let getConfiguration =
    let (|Int|_|) (str: string) =
       match System.Int32.TryParse(str) with
       | (true,int) -> Some(int)
       | _ -> None
    let configMap =
        getEnvVarsDict1 "TwitchBotUsername"
        |> getEnvVarsDict2 "TwitchBotPassword"
        |> getEnvVarsDict2 "TwitchBotHost"
        |> getEnvVarsDict2 "TwitchBotPort"
    match configMap with
    | Error x -> Error x
    | Ok m ->
        let port = match m.Item "TwitchBotPort" with
                    | Int i -> i
                    | _ -> 9243 // default port
        Ok({
            username = (m.Item "TwitchBotUsername");
            password = (m.Item "TwitchBotPassword");
            host = (m.Item "TwitchBotHost");
            port = port
        })