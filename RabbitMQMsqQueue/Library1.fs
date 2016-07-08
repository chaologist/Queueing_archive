module RabbitMQMsqQueue.Clients
open RabbitMQ.Client
open RabbitMQ.Client.Events
open PipelineMsgQueue.QueueFlow
open PipelineMsgQueue.Stepper

type Credentials = {UserName:string;Password:string}
type QueueEnvironment ={Host:string;Credentials:Credentials option;Hosts:seq<string>;VirtualHost:string option}
type OutputRouting =  {ExchangeName:string;Routing:string}
type InputQueue ={ExchangeName:string;QueueName:string;Routings:seq<string>}
type OutputConfig = {Environment:QueueEnvironment; OutputRouting:OutputRouting} 
type ClientConfig<'a,'b> = {OutputConfig:OutputConfig; InputEnvironment:QueueEnvironment; InputQueue:InputQueue; Work:QueueWork<'a,'b>}


let msgToTrace msg=
    System.Diagnostics.Trace.WriteLine msg
    ()

let MakeConnectionFactory environment=
    let factory = new ConnectionFactory()
    factory.HostName<-environment.Host
    match environment.VirtualHost with
        | Some (vhost)->
            factory.VirtualHost <- vhost
        | _->()
    match environment.Credentials with
        | Some (cred)->
            factory.UserName <- cred.UserName
            factory.Password<- cred.Password
        | _-> ()
    factory

let RabbitEnqueuerMaker outputConfig =
    let f rawbytes=
        let factory = MakeConnectionFactory outputConfig.Environment
        use connection = factory.CreateConnection()
        use channel = connection.CreateModel()
        let prop = channel.CreateBasicProperties()
        prop.Persistent<-true
        channel.ExchangeDeclare (outputConfig.OutputRouting.ExchangeName,"topic");
        channel.BasicPublish(outputConfig.OutputRouting.ExchangeName,outputConfig.OutputRouting.Routing,prop,rawbytes);
        rawbytes

    f

let MakeRabbitClient clientConfig=
    let outputEnqueuer = RabbitEnqueuerMaker clientConfig.OutputConfig
    let factory =MakeConnectionFactory clientConfig.InputEnvironment
    use connection = factory.CreateConnection()
    use channel = connection.CreateModel()
    //make sure the exchange exists
    channel.ExchangeDeclare (clientConfig.InputQueue.ExchangeName,"topic");
    //ensure the queue exists as a durable queue
    let decRes=channel.QueueDeclare(clientConfig.InputQueue.QueueName,true,false,false,null)
    //bind my queue
    clientConfig.InputQueue.Routings |> Seq.iter (fun r->
                                                    channel.QueueBind (clientConfig.InputQueue.QueueName,clientConfig.InputQueue.ExchangeName,r)
                                                  )
    ignore(channel.BasicQos(0u, 1us,false))
    let consumer = new EventingBasicConsumer(channel)
    
    consumer.Received.Add(fun ea->
                                let ackNack = {acker=(fun x->channel.BasicAck(ea.DeliveryTag,false)
                                                             ()
                                                        );nacker=(fun x->
                                                                            ignore(channel.BasicNack(ea.DeliveryTag,false,true))
                                                                            ()
                                                        )
                                               }
                                let definition= {telemetryLogger=msgToTrace;work=clientConfig.Work;outQueues=[outputEnqueuer];ackerNacker = ackNack}                            
                                let pipe = QueueMaker definition
                                let result = ea.Body |> pipe
                                ()
    )
    consumer

