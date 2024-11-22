namespace AGTec.Common.Randomizer;

public interface IRandomRefType<out TReturn>
{
    TReturn GenerateValue();
}