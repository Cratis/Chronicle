// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Strings;

/// <summary>
/// Provides a set of extension methods to <see cref="string"/>.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Convert a string into a pascal cased string.
    /// </summary>
    /// <param name="stringToConvert">string to convert.</param>
    /// <returns>Converted string.</returns>
    public static string ToPascalCase(this string stringToConvert)
    {
        if (!string.IsNullOrEmpty(stringToConvert))
        {
            if (stringToConvert.Length == 1)
                return stringToConvert.ToUpperInvariant();

            var firstLetter = stringToConvert[0].ToString().ToUpperInvariant();
            return $"{firstLetter}{stringToConvert.Substring(1)}";
        }

        return stringToConvert;
    }

    /// <summary>
    /// <para>Convert a string into a camel cased string.</para>
    /// <para>
    /// If the string is empty or null or starts with a lower-case letter, the string is returned unchanged.
    /// Also, if the string starts with two upper-case letters (suggesting an acronym), the string is returned unchanged.
    /// </para>
    ///
    /// </summary>
    /// <param name="stringToConvert">string to convert.</param>
    /// <returns>Converted string.</returns>
    public static string ToCamelCase(this string stringToConvert)
    {
        // This implementation is adapted from the JsonCamelCaseNamingPolicy implementation in System.Text.Json
        // https://github.com/dotnet/runtime/blob/f6a96fb85f187cc749843ac1cc8186431498df18/src/libraries/System.Text.Json/tests/System.Text.Json.Tests/Serialization/NamingPolicyUnitTests.cs
#pragma warning disable CS8603 // Possible null reference return.
        return stringToConvert switch
        {
            null or "" => stringToConvert,
            _ when stringToConvert.Length >= 2 && char.IsUpper(stringToConvert[0]) &&
                   char.IsUpper(stringToConvert[1]) => stringToConvert,
            _ when !char.IsUpper(stringToConvert[0]) => stringToConvert,
            _ => ConvertToCamelCase(stringToConvert),
        };
#pragma warning restore CS8603 // Possible null reference return.
    }

    static string ConvertToCamelCase(string stringToConvert)
    {
#if NETCOREAPP
        return string.Create(stringToConvert.Length, stringToConvert, (chars, name) =>
        {
            name.CopyTo(chars);
            FixCamelCasing(chars);
        });
#else
    char[] chars = stringToConvert.ToCharArray();
    FixCasing(chars);
    return new string(chars);
#endif
    }

    /// <summary>
    /// from https://github.com/dotnet/runtime/blob/f6a96fb85f187cc749843ac1cc8186431498df18/src/libraries/System.Text.Json/Common/JsonCamelCaseNamingPolicy.cs.
    /// </summary>
    /// <param name="chars">Span of chars representing the string.</param>
    static void FixCamelCasing(Span<char> chars)
    {
        for (var i = 0; i < chars.Length; i++)
        {
            if (i == 1 && !char.IsUpper(chars[i]))
            {
                break;
            }

            var hasNext = i + 1 < chars.Length;

            // Stop when next char is already lowercase.
            if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
            {
                // If the next char is a space, lowercase current char before exiting.
                if (chars[i + 1] == ' ')
                {
                    chars[i] = char.ToLowerInvariant(chars[i]);
                }

                break;
            }

            chars[i] = char.ToLowerInvariant(chars[i]);
        }
    }
}
