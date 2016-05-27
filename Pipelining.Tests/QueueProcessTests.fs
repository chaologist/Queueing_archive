namespace Pipelining.Tests
open Microsoft.VisualStudio.TestTools.UnitTesting
open PipelineMsgQueue.QueueProcess
[<TestClass>]
type QueueProcessTests() = 
    [<TestMethod>]
    member this.QueueMessageSerialize()=
        //arrange
        let start = {Id=System.Guid.NewGuid(); StartedOnUtc = System.DateTime.UtcNow; Body=Array.zeroCreate<byte> 100}
        //act
        let serialize = BuildMessage start
        let deserialize = ParseMessage serialize.Body

        //Assert
        Assert.AreEqual(start.Id,deserialize.Id)
        Assert.AreEqual(start.StartedOnUtc,deserialize.StartedOnUtc)
        Assert.AreEqual(start.Body.Length,deserialize.Body.Length)
        Assert.IsTrue (Array.fold2 (fun acc i j -> acc && i=j)true start.Body deserialize.Body  )
