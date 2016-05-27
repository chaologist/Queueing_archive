namespace Pipelining.Tests
open Microsoft.VisualStudio.TestTools.UnitTesting
open PipelineMsgQueue.Stepper
[<TestClass>]
type StepperTests() = 
    [<TestMethod>]
    member this.StepperHappyPath()=
        //arrange
        //trivial work
        let work x = 
            "b"

        let pipe x  = 
            x |> EntryPoint |> DoStep work 

        //act
        let result = pipe 1

        //Assert
        match result with 
        | Success(x) when x="b" ->
           ()
        |_-> 
            Assert.Fail("Expected \"b\"")
        ()

    [<TestMethod>]
    member this.StepperFailPath()=
        //arrange
        //trivial work
        let work x = 
            raise (new System.Exception("some message"))

        let pipe x  = 
            x |> EntryPoint |> DoStep work 

        //act
        let result = pipe 1

        //Assert
        match result with 
        | Failure (ex) ->
           ()
        |_-> 
            Assert.Fail("Failure (Exception)")
        ()

    [<TestMethod>]
    member this.StepperFailPathNoExec()=
        //arrange
        //trivial work
        let lst = new System.Collections.Generic.List<int>()
        let work x = 
            lst.Add x
            new System.Guid()

        let pipe x  = 
            x |> DoStep work 

        //act
        let result = pipe (Failure (new System.Exception()))

        //Assert
        match result with 
        | Failure (ex) ->
           ()
        |_-> 
            Assert.Fail("Failure (Exception)")

        Assert.AreEqual (0,lst.Count) //prove it has not been run
        ()


