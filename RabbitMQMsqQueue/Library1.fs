namespace MessagingQueueing.Queueing.RabbitMQClients
open RabbitMQ.Client
open RabbitMQ.Client.Events
open PipelineMsgQueue.QueueFlow
open PipelineMsgQueue.Stepper
open PipelineMsgQueue.QueueProcess
open internalShared

type RabbitMQEnqueuer<'a> (config:OutputConfig)=
    let outputEnqueuer = RabbitEnqueuerMaker config
    interface Messaging.Queueing.IEnqueuer<'a> with  
        member this.Enqueue(value:'a)=
            let n =  EnqueuerMaker msgToTrace [outputEnqueuer]
            let g =PipelineMsgQueue.QueueProcess.NewtonsoftJsonSerializer 
                    >> (fun b-> {Id=System.Guid.NewGuid();StartedOnUtc=System.DateTime.UtcNow;Body=b;CurrentStep=QueueSteps.ResultSerialized})
                    >> BuildMessage
            let r =g value
            let gg =(n r.Body)
            ()    
        member this.Dispose()=
            ()

type RabbitMQClient<'a,'b> (config:ClientConfig<'a,'b>) =

    let outputEnqueuer = RabbitEnqueuerMaker config.OutputConfig
    let factory =MakeConnectionFactory config.InputEnvironment

    let mutable connection:IConnection=null
    let mutable channel:IModel = null 
    let mutable consumer:EventingBasicConsumer = null

    let MakeRabbitClient clientConfig=
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

    interface Messaging.Queueing.IQueueClient with
        member this.AddExceptionHandler(hldr)=
            ()
        member this.Dispose()=
            channel.Dispose()
            connection.Dispose()
            ()
        member this.Start()=
            connection <- factory.CreateConnection()
            channel <- connection.CreateModel()
            consumer<-MakeRabbitClient config
            ignore(channel.BasicConsume(config.InputQueue.QueueName,false,consumer))
            ()


