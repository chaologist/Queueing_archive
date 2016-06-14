module PipelineMsgQueue.Logging

type LogEnvironment = {OccuredOn:System.DateTime; Machine:string;Application:string;}
type StartEvent<'b> ={Environment:LogEnvironment;State:'b}
type EndEvent = {Environment:LogEnvironment;Duration:int64}
type ExceptionEvent ={Environment:LogEnvironment;Exception:System.Exception}
type TraceEvent<'b>=
    | Start of StartEvent<'b>
    | End of EndEvent
    | Exception of ExceptionEvent

let serializeAndLog messageConsumer msg=
    let serialize = Newtonsoft.Json.JsonConvert.SerializeObject msg
    messageConsumer serialize

let AddLogging messageConsumer f x=
    let logger = serializeAndLog messageConsumer
    try
        let sw = new System.Diagnostics.Stopwatch()
        sw.Start()
        logger (Start({Environment={OccuredOn = System.DateTime.UtcNow; Application=System.AppDomain.CurrentDomain.FriendlyName;Machine = System.Environment.MachineName};State=x}))
        
        let res = f x
        sw.Stop()
        logger (End({Environment={OccuredOn = System.DateTime.UtcNow; Application=System.AppDomain.CurrentDomain.FriendlyName;Machine = System.Environment.MachineName}; Duration=sw.ElapsedMilliseconds}))
        res
    with e->
        logger (Exception({Environment={OccuredOn = System.DateTime.UtcNow; Application=System.AppDomain.CurrentDomain.FriendlyName;Machine = System.Environment.MachineName};Exception=e}))
        raise e

let AddLoggingToTrace f x =
    AddLogging (fun m->System.Diagnostics.Trace.WriteLine m) f x

let CreateTraceMessageConsumer () =
    fun msg -> System.Diagnostics.Trace.WriteLine msg



