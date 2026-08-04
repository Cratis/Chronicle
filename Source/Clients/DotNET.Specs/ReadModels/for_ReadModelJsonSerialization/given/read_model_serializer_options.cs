// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.ReadModels.for_ReadModelJsonSerialization.given;

public class read_model_serializer_options : Specification
{
    protected JsonSerializerOptions _options;

    void Establish() => _options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    }.WithDeclaredCollectionsNeverNull();

    protected T Read<T>(string json) => JsonSerializer.Deserialize<T>(json, _options)!;
}
