module PipelineMsgQueue.Logging

type LogMessage = {OccuredOn:System.DateTime; Machine:string;Application:string}

type TraceEvent=
    | Start of LogMessage
    | End of LogMessage*int64
    | Exception of LogMessage*System.Exception

let serializeAndLog messageConsumer msg=
    let serialize = Newtonsoft.Json.JsonConvert.SerializeObject msg
    messageConsumer serialize

let AddLogging messageConsumer f x=
    let logger = serializeAndLog messageConsumer
    try
        let sw = new System.Diagnostics.Stopwatch()
        sw.Start()
        logger (Start({OccuredOn = System.DateTime.UtcNow; Application=System.AppDomain.CurrentDomain.FriendlyName;Machine = System.Environment.MachineName}))
        
        let res = f x
        sw.Stop()
        logger (End({OccuredOn = System.DateTime.UtcNow; Application=System.AppDomain.CurrentDomain.FriendlyName;Machine = System.Environment.MachineName},sw.ElapsedMilliseconds))
        res
    with e->
        logger (Exception({OccuredOn = System.DateTime.UtcNow; Application=System.AppDomain.CurrentDomain.FriendlyName;Machine = System.Environment.MachineName},e))
        raise e

let AddLoggingToTrace f x =
    AddLogging (fun m->System.Diagnostics.Trace.WriteLine m) f x

let CreateTraceMessageConsumer () =
    fun msg -> System.Diagnostics.Trace.WriteLine msg



