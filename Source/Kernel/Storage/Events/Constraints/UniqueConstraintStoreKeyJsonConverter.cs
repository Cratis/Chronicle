// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Rejects automatic JSON conversion of secret key material without inspecting or disclosing its value.
/// </summary>
/// <remarks>
/// This is a fail-closed serialization boundary, not a persistence codec. Persistence requires explicit copy-out.
/// </remarks>
public sealed class UniqueConstraintStoreKeyJsonConverter : JsonConverter<UniqueConstraintStoreKey>
{
    /// <inheritdoc/>
    public override bool HandleNull => true;

    /// <inheritdoc/>
    public override UniqueConstraintStoreKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("Unique-constraint store key material cannot be serialized or deserialized as JSON.");

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, UniqueConstraintStoreKey value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Unique-constraint store key material cannot be serialized or deserialized as JSON.");
}
