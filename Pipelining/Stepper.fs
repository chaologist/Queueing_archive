module PipelineMsgQueue.Stepper
type StepResult<'TSuccess,'TFail> =
   | Success of 'TSuccess
   | Failure of 'TFail

// convert a single value into a two-track result
let succeed x = 
    Success x

// convert a single value into a two-track result
let fail x = 
    Failure x

// apply either a success function or failure function
let either successFunc failureFunc twoTrackInput =
    match twoTrackInput with
    | Success s -> successFunc s
    | Failure f -> failureFunc f

// convert a switch function into a two-track function
let bind f = 
    either f fail

// compose two switches into another switch
let (>=>) s1 s2 = 
    s1 >> bind s2

// convert a one-track function into a switch with exception handling
let tryCatch f exnHandler x =
    try
        f x |> succeed
    with
    | ex -> exnHandler ex |> fail

//start a value into the pipe
let EntryPoint x=
    Success x

let DoStep (f:'a->'b) previous =
    let tc = tryCatch f (fun ex->ex)
    match previous with
    | Success s-> s|>tc
    | Failure f->  Failure (f)
