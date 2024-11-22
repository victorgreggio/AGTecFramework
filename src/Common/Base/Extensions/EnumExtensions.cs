using System;
using System.ComponentModel;
using System.Linq;

namespace AGTec.Common.Base.Extensions;

public static class EnumExtensions
{
    public static string GetDescriptionOfEnum(this Enum value)
    {
        var type = value.GetType();
        if (!type.IsEnum) throw new ArgumentException($"Type '{type}' is not Enum");

        var members = type.GetMember(value.ToString());
        if (members.Length == 0) throw new ArgumentException($"Member '{value}' not found in type '{type.Name}'");

        var member = members[0];
        var attributes = member.GetCustomAttributes(typeof(DescriptionAttribute), false);
        if (attributes.Length == 0)
            throw new ArgumentException(
                $"'{type.Name}.{value}' doesn't have DisplayAttribute");

        var attribute = (DescriptionAttribute)attributes[0];
        return attribute.Description;
    }

    public static T GetEnumValueFromDescription<T>(this string value)
    {
        var type = typeof(T);

        if (!type.IsEnum) throw new InvalidOperationException();

        foreach (var field in type.GetFields())
            if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
            {
                if (attribute.Description == value)
                    return (T)field.GetValue(null);
            }
            else
            {
                if (field.Name == value)
                    return (T)field.GetValue(null);
            }

        throw new ArgumentException("Not found.", nameof(value));
    }

    public static T Next<T>(this T src) where T : struct
    {
        if (typeof(T).IsEnum == false)
            throw new ArgumentException($"Argumnent {typeof(T).FullName} is not an Enum.");

        var values = Enum.GetValues(src.GetType()).Cast<T>().ToArray();
        var newValueIndex = Array.IndexOf(values, src) + 1;

        if (values.Length == newValueIndex)
            throw new ArgumentException($"There is no next value for {typeof(T).FullName}.");

        return values[newValueIndex];
    }
}