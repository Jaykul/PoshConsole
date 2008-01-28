using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MbUnit.Framework;
using PoshConsole;
namespace UnitTests
{
    [TestFixture]
    public class UtilitiesClass
    {
        [Test]
        public void LineCountIsCorrect()
        {
            Assert.AreEqual(3,
@"This is silly,
But after all, 
most tests will be.".LineCount());
        }
    }
}
