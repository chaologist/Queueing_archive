module PipelineMsgQueue.QueueFlow
open PipelineMsgQueue.Enqueuer
open PipelineMsgQueue.QueueProcess
open PipelineMsgQueue.Stepper
type QueueWork<'a,'b> = {deserializer:byte[]->'a;serializer:'b->byte[];work:'a->'b}
type AckerOrNacker = {acker:unit->unit;nacker:unit->unit} //need to change this definition...
type QueueDefinition<'a,'b> = {telemetryLogger:string->unit; work:QueueWork<'a,'b>; ackerNacker:AckerOrNacker; outQueues:seq<byte[]->byte[]> }

let MakeJsonSerializedQueueWork work =
    {deserializer=NewtonsoftJsonDeserializer;serializer=NewtonsoftJsonSerializer; work=work}

let QueueMaker<'a,'b> (definition:QueueDefinition<'a,'b>)=
    let outboundQueuer = Enqueuer definition.telemetryLogger definition.outQueues
    let queueProcess = ProcessMessage  definition.telemetryLogger definition.work.deserializer definition.work.serializer definition.work.work 
    let ackNack = AckerNacker definition.ackerNacker.acker definition.ackerNacker.nacker
    //need to work on acknack...  needs to take messages
    let flow bytes = 
        bytes |> queueProcess |> outboundQueuer |> (ackNack)
    
    flow
