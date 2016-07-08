module PipelineMsgQueue.Enqueuer
open PipelineMsgQueue.Logging
open PipelineMsgQueue.Stepper
open PipelineMsgQueue.QueueProcess

let Enqueuer messageConsumer (outQueues:seq<byte[]->byte[]>) (message:StepResult<QueueMessageStep<byte[]>,exn>)=
    let augmentedOutQueues = Seq.append ([fun x->x]) outQueues
    let queuers = augmentedOutQueues |> Seq.map (fun q-> q |> (messageConsumer |> TraceQueueStep))
    let result = queuers |> Seq.map (fun q-> async{ return (q message)})
    let g = result |> Async.Parallel |> Async.RunSynchronously

    g |> Array.fold resultReducer message

