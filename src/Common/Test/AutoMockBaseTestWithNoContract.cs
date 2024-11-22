namespace AGTec.Common.Test;

public abstract class AutoMockBaseTestWithNoContract<TSut>
    : AutoMockBaseTest<TSut, TSut> where TSut : class
{
}