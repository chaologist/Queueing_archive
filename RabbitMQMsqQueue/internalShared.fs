module internalShared
open RabbitMQ.Client
open PipelineMsgQueue.QueueFlow
open PipelineMsgQueue.Stepper
open MessagingQueueing.Queueing.RabbitMQClients

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



