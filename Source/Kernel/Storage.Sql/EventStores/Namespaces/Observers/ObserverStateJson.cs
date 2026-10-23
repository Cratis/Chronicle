// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;

/// <summary>
/// Shared serializer options for observer control and initialization JSON columns.
/// </summary>
internal static class ObserverStateJson
{
    /// <summary>
    /// Gets serializer options that store concepts as their underlying values.
    /// </summary>
    internal static JsonSerializerOptions Options { get; } = new()
    {
        Converters = { new ConceptAsJsonConverterFactory(), new Concepts.Keys.KeyJsonConverter() }
    };
}
