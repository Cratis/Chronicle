// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.given;

/// <summary>
/// Simple test naming policy to avoid NSubstitute argument matcher issues.
/// </summary>
internal class TestNamingPolicy : INamingPolicy
{
    public string GetPropertyName(string name) => name;
    public string GetPropertyName(Properties.PropertyPath propertyPath) => propertyPath?.Path ?? string.Empty;
    public string GetReadModelName(Type readModelType) => readModelType.Name;
    public JsonNamingPolicy? JsonPropertyNamingPolicy => JsonNamingPolicy.CamelCase;
}
