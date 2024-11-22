namespace AGTec.Common.Base.ValueObjects;

public interface ISingleValueObject<out T>
{
    T GetValue();
}