using System;
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

    protected override void BeforeCreateSut()
    {
        GivenThat();
    }

    protected override void AfterCreateSut()
    {
        AndGivenThatAfterCreated();

        if (IsCreation) return;

        var whenIRun = WhenIRun;

        if (CatchExceptionOnRunning) whenIRun = () => RegisterException(WhenIRun);

        whenIRun();
    }

    protected override void AfterEachTest()
    {
        AndThenCleanUp();
    }

    protected virtual void GivenThat()
    {
    }

    protected virtual void AndGivenThatAfterCreated()
    {
    }

    protected virtual void WhenIRun()
    {
        throw new NotImplementedException(
            "Please implement WhenIRun in the derived class or use ConstructorSpecification attrubute in the class");
    }

    protected virtual void AndThenCleanUp()
    {
    }

    protected void RegisterException(Action action)
    {
        try
        {
            action();
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