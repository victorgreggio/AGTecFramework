using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AGTec.Common.Test;

[TestClass]
public abstract class BaseTest
{
    [TestInitialize]
    public virtual async Task SetUp()
    {
        await BeforeEachTest();
    }

    [TestCleanup]
    public async Task TearDown()
    {
        await AfterEachTest();
    }

    protected virtual Task BeforeEachTest()
    {
        return Task.CompletedTask;
    }

    protected virtual Task AfterEachTest()
    {
        return Task.CompletedTask;
    }
}