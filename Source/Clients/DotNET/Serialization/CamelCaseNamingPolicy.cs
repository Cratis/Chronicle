// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Strings;

namespace Cratis.Chronicle.Serialization;

/// <summary>
/// Default implementation of <see cref="INamingPolicy"/>.
/// </summary>
public class CamelCaseNamingPolicy : INamingPolicy
{
    /// <inheritdoc/>
    public string ConvertName(string name) => name.ToCamelCase();
}
