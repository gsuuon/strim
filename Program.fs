open System
open System.Diagnostics
open System.Text.RegularExpressions

Console.OutputEncoding <- Text.Encoding.UTF8

type ProcResult = {
  out : string
  err : string
  code : int
}

/// NOTE this will stall forever if proc waits for input with a message / and may not show message (if it isn't newlined)
let exec handleOut handleErr (cmd: string) =
  let processStartInfo = new ProcessStartInfo()

  processStartInfo.FileName <- "cmd.exe"
  processStartInfo.Arguments <- sprintf "/C %s" cmd
  processStartInfo.UseShellExecute <- false
  processStartInfo.RedirectStandardOutput <- true
  processStartInfo.RedirectStandardError <- true
  processStartInfo.RedirectStandardInput <- true

  let proc = new Process()
  proc.StartInfo <- processStartInfo
  proc.Start() |> ignore

  let mutable errLines = []
  let mutable outLines = []

  async {
    let stdin = proc.StandardInput

    async {
      use errReader = proc.StandardError

      while not proc.HasExited do
        if not errReader.EndOfStream then
          let err = errReader.ReadLine()
          errLines <- errLines @ [err]
          handleErr err
    } |> Async.Start

    async {
      while not proc.HasExited do
        let line = Console.ReadLine()
        stdin.WriteLine(line)
    } |> Async.Start

    async {
      use reader = proc.StandardOutput

      while not proc.HasExited do
        if not reader.EndOfStream then
          let output = reader.ReadLine()
          outLines <- outLines @ [output]
          handleOut output
    } |> Async.Start
  } |> Async.Start

  Console.CancelKeyPress.Add(fun e ->
    e.Cancel <- true
  )

  proc.WaitForExit() |> ignore

  {
    out = String.concat "\n" outLines
    err = String.concat "\n" errLines
    code = proc.ExitCode
  }

// We're sending everything except output file to stderr so stdout is just the output file name
let show = exec (eprintfn "%s") (eprintfn "%s")
let hide = exec ignore ignore

let sleep ms = Threading.Thread.Sleep 1000

let twitchKey =
  match Environment.GetEnvironmentVariable "TWITCH_STREAM_KEY" with
  | null ->
    failwithf "Missing TWITCH_STREAM_KEY"
  | x -> x

let ingestUrl =
  let defaultUrl = "rtmp://live.twitch.tv/app"

  match
    Environment.GetCommandLineArgs()
    |> Array.tryItem 1
  with
  | Some ingestUrl ->
    ingestUrl
  | None ->
    printfn "Defaulting to %s" defaultUrl
    defaultUrl

printfn "Going live in 3.."
sleep 1000
printfn "2.."
sleep 1000
printfn "1.."
sleep 1000
printfn "🔦"

type StreamProgress = {
  bitrate : string
  fps : string
  uptime : string
}

let twitch cmd =
  let trim (x: string) = x.Trim()

  let parseStatus out =
    Regex.Match
      ( out
      , ".+fps=(?<fps>[0-9 .]+).+size=(?<size>.+)time=(?<time>[0-9:.]+).*bitrate=(?<bitrate>.+/s)."
      )

  let handleProgress =
    let mutable started = false

    fun out ->
      match parseStatus out with
      | capture when capture.Success -> 
        if started = false
          && trim capture.Groups.["size"].Value <> "0kb"
        then
          started <- true
          printfn "🎥"

        let v (name: string) = capture.Groups.[name].Value

        let status =
          sprintf "[uptime %s] [fps %s] [bitrate %s]"
            (v "time")
            (v "fps")
            (v "bitrate")
            
        Console.Write status
        Console.Write "\r"
      | _ -> ()

  exec
  <| eprintfn "%s"
  <| handleProgress
  <| cmd
  |> ignore

  printfn ""

twitch $"""ffmpeg -f gdigrab -framerate 30 -i desktop -c:v libx264 -preset veryfast -maxrate 3000k -bufsize 6000k -pix_fmt yuv420p -g 60 -c:a aac -b:a 128k -f flv {ingestUrl}/{twitchKey}"""

printfn "🎬"
