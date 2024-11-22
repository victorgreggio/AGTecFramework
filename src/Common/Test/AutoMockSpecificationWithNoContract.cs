namespace AGTec.Common.Test;

public abstract class AutoMockSpecificationWithNoContract<TSut>
    : AutoMockSpecification<TSut, TSut> where TSut : class
{
}