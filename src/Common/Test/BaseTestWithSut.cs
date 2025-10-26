using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AGTec.Common.Test;

[TestClass]
public abstract class BaseTestWithSut<TSut>
    : BaseTest
{
    protected TSut Sut { get; set; }

    [TestInitialize]
    public override async Task SetUp()
    {
        await base.SetUp();

        await BeforeCreateSut();

        Sut = CreateSut();

        await AfterCreateSut();
    }

    protected virtual Task BeforeCreateSut()
    {
        return Task.CompletedTask;
    }

    protected virtual Task AfterCreateSut()
    {
        return Task.CompletedTask;
    }

    protected abstract TSut CreateSut();
}