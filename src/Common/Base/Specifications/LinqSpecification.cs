using System;
using System.Linq.Expressions;

namespace AGTec.Common.Base.Specifications;

public abstract class LinqSpecification<T> : CompositeSpecification<T>
{
    public abstract Expression<Func<T, bool>> AsExpression();

    public override bool IsSatisfiedBy(T candidate)
    {
        return AsExpression().Compile()(candidate);
    }
}