using System;
using System.Linq;
using AGTec.Common.Randomizer.ReferenceTypes;
using AGTec.Common.Randomizer.Validation;

namespace AGTec.Common.Randomizer.Impl;

public class RandomAlphanumericStringGenerator : RandomStringGeneratorBase, IRandomAlphanumericString
{
    public RandomAlphanumericStringGenerator()
    {
    }

    public RandomAlphanumericStringGenerator(int seed)
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

        return GenerateRandomString(length, Constants.AlphanumericCharacters.ToCharArray());
    }

    public string GenerateLowerCaseValue(int length = 25)
    {
        Validator.ValidateCondition(length, item => item > 0);
        return GenerateRandomString(length, Constants.Lowercase.ToCharArray());
    }

    public string GenerateUpperCaseValue(int length = 25)
    {
        if (length <= 0) throw new ArgumentException("Length cannot be less or equal 0.");

        return GenerateRandomString(length, Constants.Uppercase.ToCharArray());
    }

    public string GenerateApartFrom(int length, params char[] excluded)
    {
        Validator.ValidateCondition(length, item => item > 0);
        Validator.ValidateNull(excluded);
        Validator.ValidateCondition(excluded, item => item.Length > 0);

        var alphanumericList = Constants.AlphanumericCharArray.ToList();
        var itemsWithoutExcluded = alphanumericList.Except(excluded).ToList();

        return GenerateRandomString(length, itemsWithoutExcluded);
    }

    protected override string GetRandomValue()
    {
        return GenerateRandomString(25, Constants.AlphanumericCharacters.ToCharArray());
    }
}