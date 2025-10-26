using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AGTec.Common.Test;

public abstract class AutoMockSpecification<TSut, TContract>
    : AutoMockBaseTest<TSut, TContract> where TSut : class, TContract
{
    protected AutoMockSpecification()
    {
        CatchExceptionOnRunning = HasAttribute<ExceptionSpecificationAttribute>();

        IsCreation = HasAttribute<ConstructorSpecificationAttribute>();
    }

    protected Exception ExceptionThrown { get; set; }

    protected bool IsCreation { get; set; }

    protected bool CatchExceptionOnRunning { get; set; }

    protected override async Task BeforeCreateSut()
    {
        await GivenThat();
    }

    protected override async Task AfterCreateSut()
    {
        await AndGivenThatAfterCreated();

        if (IsCreation) return;

        if (CatchExceptionOnRunning)
            await RegisterExceptionAsync(WhenIRun);
        else
            await WhenIRun();
    }

    protected override async Task AfterEachTest()
    {
        await AndThenCleanUp();
    }

    protected virtual Task GivenThat()
    {
        return Task.CompletedTask;
    }

    protected virtual Task AndGivenThatAfterCreated()
    {
        return Task.CompletedTask;
    }

    protected virtual Task WhenIRun()
    {
        throw new NotImplementedException(
            "Please implement WhenIRun in the derived class or use ConstructorSpecification attrubute in the class");
    }

    protected virtual Task AndThenCleanUp()
    {
        return Task.CompletedTask;
    }

    protected async Task RegisterExceptionAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception e)
        {
            ExceptionThrown = e;
        }
    }

    protected void AssertExceptionThrown()
    {
        AssertExceptionThrown<Exception>();
    }

    protected void AssertExceptionThrown<T>() where T : Exception
    {
        Assert.IsInstanceOfType(ExceptionThrown, typeof(T));
    }

    protected bool HasAttribute<T>() where T : Attribute
    {
        var attributes = GetType().GetCustomAttributes(typeof(T), true);

        return attributes.Length > 0;
    }
}