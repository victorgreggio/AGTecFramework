namespace AGTec.Common.Randomizer.ReferenceTypes;

public interface IRandomCharacter : IRandomRefType<char>
{
    char GenerateValue(char min, char max);
}