open MessagingQueueing.Queueing.RabbitMQClients
[<EntryPoint>]
let main argv = 
    let env = {Host="localhost";Credentials=None;Hosts=[|"localhost"|];VirtualHost=None}
    let inq = {ExchangeName="twitter";QueueName="RawTweet";Routings=[|"*.Raw.*"|]}
    let outRouting = {ExchangeName="twitter";Routing=".Parsed."}
    let outConfig ={Environment=env;OutputRouting = outRouting}

    use quer = new RabbitMQEnqueuer<string> ({Environment=env;OutputRouting={ExchangeName="twitter";Routing=".Raw."}}) :> Messaging.Queueing.IEnqueuer<string>
    quer.Enqueue "Hello World"

    let work (i:string)=
        i
    let config = {OutputConfig=outConfig; InputEnvironment=env; InputQueue=inq; Work={deserializer=(fun a->"blah");work=work;serializer=(fun b->[||])}}
    let n = new RabbitMQClient<string,string>(config) :> Messaging.Queueing.IQueueClient
    n.Start()
    printfn "%A" argv
    System.Console.ReadLine()
    n.Dispose()
    0 // return an integer exit code
