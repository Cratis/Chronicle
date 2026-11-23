// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Auditing;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Provides generated metadata and serialization handlers for the fixed event metadata shapes.
/// Content, hashes and read deserialization retain the converter's existing options.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(IEnumerable<Causation>), TypeInfoPropertyName = "CausationChain")]
[JsonSerializable(typeof(IEnumerable<string>), TypeInfoPropertyName = "IdentityValues")]
[JsonSerializable(typeof(string[]), TypeInfoPropertyName = "TagValues")]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Causation))]
[JsonSerializable(typeof(CausationType))]
[JsonSerializable(typeof(IDictionary<string, string>), TypeInfoPropertyName = "CausationProperties")]
internal partial class EventMetadataJsonContext : JsonSerializerContext;
