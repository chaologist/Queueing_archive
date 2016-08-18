namespace Messaging.Queueing
type IQueueClient =     
    interface 
    inherit System.IDisposable
        abstract member Start: unit->unit
        abstract member AddExceptionHandler: (exn->unit)->unit
    end

type IEnqueuer<'a>=
    interface
    inherit System.IDisposable
        abstract member Enqueue: 'a->unit
    end

type IQueueClientFactory =
    interface
        abstract member Create: unit->IQueueClient
    end

type IEnqueuerFactory =
    interface
        abstract member Create<'a> : unit->IEnqueuer<'a>
    end

 