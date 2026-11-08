// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Integration.GraphProjections;

/// <summary>
/// Represents the naming policy of an Arc application: properties in camel case, as the projection declaration language spells them.
/// </summary>
public class CamelCaseNaming : INamingPolicy
{
    /// <inheritdoc/>
    public JsonNamingPolicy? JsonPropertyNamingPolicy => JsonNamingPolicy.CamelCase;

    /// <inheritdoc/>
    public string GetPropertyName(string name) => string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    /// <summary>
    /// Gets the name of the property a path ends in, in camel case.
    /// </summary>
    /// <param name="propertyPath">The <see cref="PropertyPath"/>.</param>
    /// <returns>The name.</returns>
    public string GetPropertyName(PropertyPath propertyPath) => GetPropertyName(propertyPath?.Path ?? string.Empty);

    /// <inheritdoc/>
    public string GetReadModelName(Type readModelType) => GetPropertyName(readModelType.Name);
}
