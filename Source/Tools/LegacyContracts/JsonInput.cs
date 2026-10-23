// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Tools.LegacyContracts;

static class JsonInput
{
    static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static T Read<T>(string json) => JsonSerializer.Deserialize<T>(json, _options)
        ?? throw new CannotEvaluate("JSON input cannot be null.");
}
