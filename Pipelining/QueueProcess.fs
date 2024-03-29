﻿module PipelineMsgQueue.QueueProcess
open PipelineMsgQueue.Logging
open PipelineMsgQueue.Stepper

type QueueSteps =
    |MessageParsed
    |BodyDeserialized
    |BodyTransformed
    |ResultSerialized
    |MessageBuilt
    |MessageSent
    |AckNacked

let NextStep currentStep =
    match currentStep with
    |MessageParsed->BodyDeserialized
    |BodyDeserialized->BodyTransformed
    |BodyTransformed->ResultSerialized
    |ResultSerialized->MessageBuilt
    |MessageBuilt->MessageSent
    |MessageSent->AckNacked
    |AckNacked->AckNacked

type QueueMessageStep<'a>= {Id:System.Guid;StartedOnUtc:System.DateTime;Body:'a;CurrentStep:QueueSteps}

let PerformQueueStep f step=
    {Id=step.Id;StartedOnUtc=step.StartedOnUtc;Body=f step.Body;CurrentStep=NextStep step.CurrentStep}

let BuildMessage resultsSerializedMessage=
    let bodyArray = [|resultsSerializedMessage.Id.ToByteArray(); resultsSerializedMessage.StartedOnUtc.Ticks |> System.BitConverter.GetBytes  ;resultsSerializedMessage.Body|] |> Seq.concat 
    {Id=resultsSerializedMessage.Id;StartedOnUtc=resultsSerializedMessage.StartedOnUtc;Body = bodyArray|>Array.ofSeq;CurrentStep=MessageBuilt}

let ParseMessage rawBytes=
    {Id=new System.Guid(rawBytes|>Array.take 16);StartedOnUtc=new System.DateTime(System.BitConverter.ToInt64(rawBytes|>Array.skip 16|>Array.take 8,0));Body = rawBytes|>Array.skip 24; CurrentStep=MessageParsed}

let TraceAndStep messageConsumer f x =
    x|>(f |>(messageConsumer|> AddLogging) |> DoStep )
     
let TraceQueueStep messageConsumer f x=
    x|> (f |>PerformQueueStep |> (messageConsumer|>TraceAndStep))

let buildOutbound messageConsumer (serializer:'b->byte[]) value=
    let ser = serializer|> (messageConsumer |>TraceQueueStep)
    let mrgBuild = BuildMessage |> (messageConsumer |>TraceAndStep)
    value |> (ser>>mrgBuild)

//this the generic function to transform a raw byte[] from the inbound queuu into the serialize message for the outbound queue
let ProcessMessage messageConsumer (deserialize:byte[]->'a) (serializer:'b->byte[]) (transformer:'a->'b) (rawBytes:byte[]) =
    let msgParse = ParseMessage |> (messageConsumer |> TraceAndStep) 
    let deser = deserialize|> (messageConsumer |> TraceQueueStep)
    let wrk = transformer|> (messageConsumer |>TraceQueueStep)
    let ser = serializer|> (messageConsumer |>TraceQueueStep)
    let mrgBuild = BuildMessage |> (messageConsumer |>TraceAndStep)
    let output = buildOutbound messageConsumer serializer

    let pipe=EntryPoint >> msgParse >> deser >> wrk >> output
    rawBytes |> pipe


let AckerNacker (acker:unit->unit) (nacker:unit->unit) (result:StepResult<QueueMessageStep<byte[]>,exn>) =
    match result with
        | Success (msg)->
            acker()
            Success (PerformQueueStep (fun x->x) msg)
        | Failure (e)->
            nacker()
            Failure (e)


let resultReducer acc elem =
    match acc with 
        | Failure(e)->
            Failure(e)
        | Success (x)->
            elem 
                           

let NewtonsoftJsonSerializer raw =
    Newtonsoft.Json.JsonConvert.SerializeObject (raw) |> System.Text.Encoding.UTF8.GetBytes  

let NewtonsoftJsonDeserializer rawbytes =  
    Newtonsoft.Json.JsonConvert.DeserializeObject<'a>(rawbytes|> System.Text.Encoding.UTF8.GetString)  

//fill in the json based serializer and deserializer
let DefaultProcess work rawBytes = ProcessMessage (fun m->System.Diagnostics.Trace.WriteLine m) NewtonsoftJsonDeserializer  NewtonsoftJsonSerializer work rawBytes



