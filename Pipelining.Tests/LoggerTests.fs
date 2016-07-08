namespace Pipelining.Tests
open Microsoft.VisualStudio.TestTools.UnitTesting
open PipelineMsgQueue.Logging
open System.Collections.Generic
open System.Linq

[<TestClass>]
type LoggerTests() = 
    [<TestMethod>]
    member this.LoggerHappyPath()=
        //arrange
        let messages = new System.Collections.Generic.List<string>()
        let consumer msg=
            messages.Add msg
        //trivial work
        let work x = 
            x

        let logger = AddLogging consumer work

        //act
        let result = logger "thisisa test"

        //Assert
        Assert.AreEqual ("thisisa test", result)
        Assert.AreEqual (2,messages.Count)
        Assert.AreEqual (1,messages.Where(fun x->x.Contains("Start")).Count())
        Assert.AreEqual (1,messages.Where(fun x->x.Contains("End")).Count())
        Assert.AreEqual (0,messages.Where(fun x->x.Contains("Exc")).Count())
        ()

    [<TestMethod>]
    member this.LoggerException()=
        //arrange
        let messages = new System.Collections.Generic.List<string>()
        let consumer msg=
            messages.Add msg
        //work that throws
        let work x = 
            raise (new System.Exception())

        let logger = AddLogging consumer work

        //act
        try
            let result = logger "thisisa test"
            ()
        with ex->
            //Assert
            Assert.AreEqual (2,messages.Count)
            Assert.AreEqual (1,messages.Where(fun x->x.Contains(":\"Start\"")).Count())
            Assert.AreEqual (0,messages.Where(fun x->x.Contains("End")).Count())
            Assert.AreEqual (1,messages.Where(fun x->x.Contains("Exc")).Count())
            ()

    [<TestMethod>]
    [<ExpectedException(typedefof<System.Exception>)>]
    member this.LoggerExceptionThrows()=
        //arrange
        let messages = new System.Collections.Generic.List<string>()
        let consumer msg=
            messages.Add msg
        //trivial work
        let work x = 
            raise (new System.Exception())

        let logger = AddLogging consumer work

        //act
        let result = logger "thisisa test"

        ()