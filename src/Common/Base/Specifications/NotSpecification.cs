namespace AGTec.Common.Base.Specifications;

public class NotSpecification<T> : CompositeSpecification<T>
{
    private readonly ISpecification<T> _other;

    public NotSpecification(ISpecification<T> other)
    {
        _other = other;
    }

    public override bool IsSatisfiedBy(T candidate)
    {
        return !_other.IsSatisfiedBy(candidate);
    }
}