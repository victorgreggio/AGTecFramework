using System.Linq;
using AGTec.Common.Randomizer.ReferenceTypes;
using AGTec.Common.Randomizer.Validation;

namespace AGTec.Common.Randomizer.Impl;

public class RandomStringGenerator : RandomStringGeneratorBase, IRandomString
{
    public RandomStringGenerator()
    {
    }

    public RandomStringGenerator(int seed)
        : base(seed)
    {
    }

    public string GenerateValue()
    {
        return GetRandomValue();
    }

    public string GenerateValue(int length)
    {
        Validator.ValidateCondition(length, item => item > 0);

        return GenerateStringValue(Constants.FirstCharacterHex, Constants.LastCharacterHex, length);
    }

    public string GenerateLowerCaseValue(int length = 25)
    {
        Validator.ValidateCondition(length, item => item > 0);

        var randomString = GenerateStringValue(Constants.FirstCharacterHex, Constants.LastCharacterHex, length);
        return randomString.ToLower();
    }

    public string GenerateUpperCaseValue(int length = 25)
    {
        Validator.ValidateCondition(length, item => item > 0);

        var randomString = GenerateStringValue(Constants.FirstCharacterHex, Constants.LastCharacterHex, length);
        return randomString.ToUpper();
    }

    public string GenerateApartFrom(int length = 25, params char[] excluded)
    {
        Validator.ValidateCondition(length, item => item > 0);
        Validator.ValidateNull(excluded);
        Validator.ValidateCondition(excluded, item => item.Length > 0);

        var charsAsInt = excluded.Select(item => (int)item);
        var randomString = GenerateStringValue(Constants.FirstCharacterHex, Constants.LastCharacterHex, length,
            charsAsInt.ToArray());
        return randomString;
    }

    protected override string GetRandomValue()
    {
        return GenerateStringValue(Constants.FirstCharacterHex, Constants.LastCharacterHex);
    }
}