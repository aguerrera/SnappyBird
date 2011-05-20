// Learn more about F# at http://fsharp.net

namespace SnappyBird.Tests


open NUnit.Framework

[<TestFixture>]
type myFixture() = 
    class
        [<Test>]
        member self.``test number 1``() =
            let v1 = 1
            let v2 = 2
            Assert.AreEqual(v1, v2, "huh")
    end
