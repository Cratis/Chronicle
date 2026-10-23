// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Json;

namespace Cratis.Chronicle.Observation.Jobs;

internal static class ReplaySerialization
{
    internal static JsonSerializerOptions Options() => new()
    {
        Converters = { new ConceptAsJsonConverterFactory(), new KeyJsonConverter() }
    };

    internal static T Restore<T>(T state) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(state, Options()), Options())!;
}
