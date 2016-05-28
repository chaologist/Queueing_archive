module PipelineMsgQueue.QueueFlow
open PipelineMsgQueue.Enqueuer
open PipelineMsgQueue.QueueProcess
open PipelineMsgQueue.Stepper
type QueueWork<'a,'b> = {deserializer:byte[]->'a;serializer:'b->byte[];work:'a->'b}
type AckerOrNacker<'b> = {acker:'b->StepResult<'b,exn>;nacker:exn->StepResult<'b,exn>}
type QueueDefinition<'a,'b> = {telemetryLogger:string->unit; work:QueueWork<'a,'b>; ackerNacker:AckerOrNacker<QueueMessageStep<byte[]>>; outQueues:seq<byte[]->byte[]> }

let QueueMaker<'a,'b> (definition:QueueDefinition<'a,'b>)=
    let outboundQueuer = Enqueuer definition.telemetryLogger definition.outQueues
    let queueProcess = ProcessMessage  definition.telemetryLogger definition.work.deserializer definition.work.serializer definition.work.work 
    let ackNack = AckerNacker definition.ackerNacker.acker definition.ackerNacker.nacker
    //need to work on acknack...  needs to take messages
    let flow bytes = 
        bytes |> queueProcess |> outboundQueuer |> (ackNack)
    
    flow
