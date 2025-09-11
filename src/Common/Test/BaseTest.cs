using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AGTec.Common.Test;

[TestClass]
public abstract class BaseTest
{
    [TestInitialize]
    public virtual void SetUp()
    {
        BeforeEachTest();
    }

    [TestCleanup]
    public void TearDown()
    {
        AfterEachTest();
    }

    protected virtual void BeforeEachTest()
    {
    }

    protected virtual void AfterEachTest()
    {
    }
}