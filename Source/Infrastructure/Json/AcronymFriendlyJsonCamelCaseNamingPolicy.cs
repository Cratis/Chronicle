// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Strings;

namespace Cratis.Json;

/// <summary>
/// A <see cref="JsonNamingPolicy"/> that converts names to camel case, but also takes acronyms into consideration.
/// </summary>
public class AcronymFriendlyJsonCamelCaseNamingPolicy : JsonNamingPolicy
{
    /// <summary>
    /// A singleton instance of the <see cref="AcronymFriendlyJsonCamelCaseNamingPolicy"/>.
    /// </summary>
    public static readonly JsonNamingPolicy Instance = new AcronymFriendlyJsonCamelCaseNamingPolicy();

    /// <inheritdoc/>
    public override string ConvertName(string name) => name.ToCamelCase();
}
