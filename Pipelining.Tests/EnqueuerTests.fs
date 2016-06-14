namespace Pipelining.Tests
open Microsoft.VisualStudio.TestTools.UnitTesting
open PipelineMsgQueue.QueueProcess
open PipelineMsgQueue.Enqueuer
open PipelineMsgQueue.Stepper
[<TestClass>]
type EnqueuerTests() = 
    [<TestMethod>]
    member this.EnqueuerSingleSuccess()=
        //arrange
        let start = {Id=System.Guid.NewGuid(); StartedOnUtc = System.DateTime.UtcNow; Body=Array.zeroCreate<byte> 100;CurrentStep=MessageBuilt}
        let pipelineResult = Success(start)
        let memory = new System.Collections.Generic.Dictionary<int,int>();
        memory.Add(1,0)
        let queueWriter = fun bytes->
                            memory.[1]<-memory.[1] + 1
                            bytes

        //act
        let result = Enqueuer (fun x->()) [|queueWriter|] pipelineResult

        //Assert
        match result with
           | Failure (ex)-> Assert.Fail()
           | _-> ()

        Assert.AreEqual(1,memory.[1])
        ()

    [<TestMethod>]
    member this.EnqueuerSingleFailure()=
        //arrange
        let start = {Id=System.Guid.NewGuid(); StartedOnUtc = System.DateTime.UtcNow; Body=Array.zeroCreate<byte> 100;CurrentStep=MessageBuilt}
        let pipelineResult = Failure(new System.Exception())
        let memory = new System.Collections.Generic.Dictionary<int,int>();
        memory.Add(1,0)
        let queueWriter = fun bytes->
                            memory.[1]<-memory.[1] + 1
                            bytes

        //act
        let result = Enqueuer (fun x->()) [|queueWriter|] pipelineResult

        //Assert
        match result with
           | Success (anything)-> Assert.Fail()
           | _-> ()

        Assert.AreEqual(0,memory.[1])//it should not have enqueued if it was a failure going in
        ()

    [<TestMethod>]
    member this.EnqueuerMultipleSuccess()=
        //arrange
        let start = {Id=System.Guid.NewGuid(); StartedOnUtc = System.DateTime.UtcNow; Body=Array.zeroCreate<byte> 100;CurrentStep=MessageBuilt}
        let pipelineResult = Success(start)
        let rnd = new System.Random()
        let count = rnd.Next(5,10)

        let memory = new System.Collections.Generic.Dictionary<int,int>();
        {1..count} |> Seq.iter (fun i-> memory.Add(i,0))
        let queueWriters = {1..count} |> Seq.map (fun i-> fun bytes->
                                                            memory.[i]<-memory.[i] + 1
                                                            bytes)
        //act
        let result = Enqueuer (fun x->()) queueWriters pipelineResult

        //Assert
        match result with
           | Failure (ex)-> Assert.Fail()
           | _-> ()

        Assert.AreEqual(1,memory.[1])
        {1..count}|>Seq.iter (fun i-> Assert.AreEqual(1,memory.[i]))
        ()

    [<TestMethod>]
    member this.EnqueuerMultipleSuccessDelays()=
        //arrange
        let start = {Id=System.Guid.NewGuid(); StartedOnUtc = System.DateTime.UtcNow; Body=Array.zeroCreate<byte> 100;CurrentStep=MessageBuilt}
        let pipelineResult = Success(start)
        let rnd = new System.Random()
        let count = rnd.Next(5,10)

        let memory = new System.Collections.Generic.Dictionary<int,int>();
        {1..count} |> Seq.iter (fun i-> memory.Add(i,0))
        let queueWriters = {1..count} |> Seq.map (fun i-> fun bytes->
                                                            memory.[i]<-memory.[i] + 1
                                                            System.Threading.Thread.Sleep (rnd.Next(100,1000))
                                                            bytes)
        //act
        let result = Enqueuer (fun x->()) queueWriters pipelineResult

        //Assert
        match result with
           | Failure (ex)-> Assert.Fail()
           | _-> ()

        Assert.AreEqual(1,memory.[1])
        {1..count}|>Seq.iter (fun i-> Assert.AreEqual(1,memory.[i]))
        ()

