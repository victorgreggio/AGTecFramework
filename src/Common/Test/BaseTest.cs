using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AGTec.Common.Test;

[TestClass]
public abstract class BaseTest
{
    [ClassInitialize]
    public void FixtureSetUp()
    {
        BeforeAllTests();
    }

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

    [ClassCleanup]
    public void FixtureTearDown()
    {
        AfterAllTests();
    }

    protected virtual void BeforeAllTests()
    {
    }

    protected virtual void BeforeEachTest()
    {
    }

    protected virtual void AfterEachTest()
    {
    }

    protected virtual void AfterAllTests()
    {
    }
}