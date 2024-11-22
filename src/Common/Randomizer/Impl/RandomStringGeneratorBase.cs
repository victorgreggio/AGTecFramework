using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AGTec.Common.Randomizer.Impl;

public abstract class RandomStringGeneratorBase : RandomGenericGeneratorBase<string>
{
    protected RandomStringGeneratorBase()
    {
    }

    protected RandomStringGeneratorBase(int seed)
        : base(seed)
    {
    }

    protected string GenerateRandomString(int lenght, IList<char> source)
    {
        var maxIndex = source.Count - 1;
        var resultArray = new char[lenght];
        var sourceAsArray = source.ToArray();

        for (var i = 0; i < lenght; i++)
        {
            var randomIndex = randomizer.Next(0, maxIndex);
            resultArray[i] = sourceAsArray[randomIndex];
        }

        return new string(resultArray);
    }

    protected string GenerateStringValue(int firstLetterCode, int lastLetterCode, int length = 25,
        params int[] excludedChars)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < length; index++)
        {
            var randomValue = randomizer.Next(firstLetterCode, lastLetterCode);
            if (IsExcluded(randomValue, excludedChars) == false)
                builder.Append((char)randomValue);
            else
                index--;
        }

        return builder.ToString();
    }

    private bool IsExcluded(int character, params int[] exluded)
    {
        if (exluded.Length == 0) return false;

        return exluded.Any(item => item == character);
    }
}