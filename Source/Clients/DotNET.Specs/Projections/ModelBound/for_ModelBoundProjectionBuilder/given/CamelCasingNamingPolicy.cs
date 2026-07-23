// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.given;

/// <summary>
/// A naming policy that camel-cases property names, so specs exercise the naming-policy round-trip that the
/// identity <see cref="TestNamingPolicy"/> cannot (e.g. the child-definition key lookup during RemovedWith routing).
/// </summary>
internal class CamelCasingNamingPolicy : INamingPolicy
{
    public string GetPropertyName(string name) => string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    public string GetPropertyName(Properties.PropertyPath propertyPath) => GetPropertyName(propertyPath?.Path ?? string.Empty);

    public string GetReadModelName(Type readModelType) => GetPropertyName(readModelType.Name);

    public JsonNamingPolicy? JsonPropertyNamingPolicy => JsonNamingPolicy.CamelCase;
}
