// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_AddChildBuilder.when_setting_from_object.given;

public class SnakeCaseNamingPolicy : INamingPolicy
{
    public JsonNamingPolicy JsonPropertyNamingPolicy => JsonNamingPolicy.SnakeCaseLower;

    public string GetReadModelName(Type readModelType) => GetPropertyName(readModelType.Name);

    public string GetPropertyName(string name) => JsonPropertyNamingPolicy.ConvertName(name);
}
