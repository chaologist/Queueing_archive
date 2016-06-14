namespace Pipelining.Tests
open Microsoft.VisualStudio.TestTools.UnitTesting
open PipelineMsgQueue.QueueProcess
open PipelineMsgQueue.Enqueuer
open PipelineMsgQueue.Stepper
open PipelineMsgQueue.QueueFlow

type InvocationCounter()=
    let mutable count=0
    member public this.Invoke()=
        count<-count+1
    member public this.Count()=
        count


[<TestClass>]
type QueueFlowTests() = 
    let minimalSerializer x =
        [||]

    let minimalDeserializer (x:byte[]) =
        0


    let identity x=
        x
    let deadend msg=
        ()
    let trivialAck x =
        Success(x)
    let trivialNack exn=
        Failure(exn)
    let makeTrivialAckNack()=
        {acker=trivialAck; nacker=trivialNack}

    let makeTrivialQueue()=
        {deserializer=minimalDeserializer;serializer=minimalSerializer; work=identity}
    
    let makeTestMessage()=
        BuildMessage  {Id=System.Guid.NewGuid();StartedOnUtc=System.DateTime.Now;Body=[||];CurrentStep=QueueSteps.MessageParsed}        

    [<TestMethod>]
    member this.QueueFlowDeserializes()=
        //arrange
        let deserCounter = new InvocationCounter()
        let deserialize(x)=
            deserCounter.Invoke()
            1
        let queueWorkDef= {deserializer=deserialize;serializer=minimalSerializer; work=identity}
        let queueDef = {telemetryLogger=deadend;work=queueWorkDef;ackerNacker=makeTrivialAckNack(); outQueues = [||] |> Seq.ofArray}

        let flow = QueueMaker queueDef

        //act
        let res = makeTestMessage().Body |> flow

        //assert
        match res with
            | Failure (e)->
                Assert.Fail(e.Message)
            | _->
                ()
        Assert.AreEqual(1,deserCounter.Count())
        ()
    [<TestMethod>]
    member this.QueueFlowSerializes()=
        //arrange
        let serCounter = new InvocationCounter()
        let serialize(x)=
            serCounter.Invoke()
            [||]
        let queueWorkDef= {deserializer=minimalDeserializer;serializer=serialize; work=identity}
        let queueDef = {telemetryLogger=deadend;work=queueWorkDef;ackerNacker=makeTrivialAckNack(); outQueues = [||] |> Seq.ofArray}

        let flow = QueueMaker queueDef

        //act
        let res = makeTestMessage().Body |> flow

        //assert
        match res with
            | Failure (e)->
                Assert.Fail(e.Message)
            | _->
                ()
        Assert.AreEqual(1,serCounter.Count())
        ()
    [<TestMethod>]
    member this.QueueFlowWorks()=
        //arrange
        let workCounter = new InvocationCounter()
        let work(x)=
            workCounter.Invoke()
            x
        let queueWorkDef= {deserializer=minimalDeserializer;serializer=minimalSerializer; work=work}
        let queueDef = {telemetryLogger=deadend;work=queueWorkDef;ackerNacker=makeTrivialAckNack(); outQueues = [||] |> Seq.ofArray}

        let flow = QueueMaker queueDef

        //act
        let res = makeTestMessage().Body |> flow

        //assert
        match res with
            | Failure (e)->
                Assert.Fail(e.Message)
            | _->
                ()
        Assert.AreEqual(1,workCounter.Count())
        ()

    [<TestMethod>]
    member this.QueueFlowAcks()=
        //arrange
        let ackCounter = new InvocationCounter()
        let acker(x)=
            ackCounter.Invoke()
            Success(x)

        let queueDef = {telemetryLogger=deadend;work=makeTrivialQueue();ackerNacker={acker=acker;nacker=trivialNack}; outQueues = [||] |> Seq.ofArray}

        let flow = QueueMaker queueDef

        //act
        let res = makeTestMessage().Body |> flow

        //assert
        match res with
            | Failure (e)->
                Assert.Fail(e.Message)
            | _->
                ()
        Assert.AreEqual(1,ackCounter.Count())
        ()

    [<TestMethod>]
    member this.QueueFlowNoNAckOnSuccess()=
        //arrange
        let nackCounter = new InvocationCounter()
        let nacker(exn)=
            nackCounter.Invoke()
            Failure(exn)

        let queueDef = {telemetryLogger=deadend;work=makeTrivialQueue();ackerNacker={acker=trivialAck;nacker=nacker}; outQueues = [||] |> Seq.ofArray}

        let flow = QueueMaker queueDef

        //act
        let res = makeTestMessage().Body |> flow

        //assert
        match res with
            | Failure (e)->
                Assert.Fail(e.Message)
            | _->
                ()
        Assert.AreEqual(0,nackCounter.Count())
        ()

    [<TestMethod>]
    member this.QueueFlowNAckOnFailure()=
        //arrange
        let nackCounter = new InvocationCounter()
        let nacker(exn)=
            nackCounter.Invoke()
            Failure(exn)

        let w = {deserializer=minimalDeserializer;serializer=minimalSerializer; work=fun x->
                                                                                        raise (new System.Exception ("ZOMG!!1"))}

        let queueDef = {telemetryLogger=deadend;work=w;ackerNacker={acker=trivialAck;nacker=nacker}; outQueues = [||] |> Seq.ofArray}

        let flow = QueueMaker queueDef

        //act
        let res = makeTestMessage().Body |> flow

        //assert
        match res with
            | Success (x)->
                Assert.Inconclusive ("Expected a Failure but got Success.")
            | _->
                ()
        Assert.AreEqual(1,nackCounter.Count())
        ()



