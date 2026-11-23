// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Helpers;

/// <summary>
/// The exception thrown for an invalid custom HTTP header, without exposing its value.
/// </summary>
/// <remarks>
/// The header name is only included when it is a valid HTTP token, so a value pasted into the name field is never echoed.
/// </remarks>
/// <param name="name">The header name.</param>
public class InvalidCustomHttpHeader(string name)
    : Exception(IsToken(name)
        ? $"Custom HTTP header '{name}' has an invalid name or value."
        : "A custom HTTP header has an invalid name.")
{
    const string TokenSymbols = "!#$%&'*+-.^_`|~";

    static bool IsToken(string name) =>
        name.Length > 0 && name.All(character => char.IsAsciiLetterOrDigit(character) || TokenSymbols.Contains(character));
}
