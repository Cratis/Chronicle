// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents a <see cref="IBsonSerializationProvider"/> for <see cref="IConstraintDefinition"/>.
/// </summary>
/// <remarks>
/// A provider rather than a serializer, because a serializer would never be reached. The driver ships a provider
/// that resolves every interface to a <c>DiscriminatedInterfaceSerializer</c>, so the serializer auto-registration
/// in <see cref="Serialization.CustomSerializers"/> sees a serializer already available for
/// <see cref="IConstraintDefinition"/> and skips its own. A provider registered afterwards is consulted first, so
/// this is what actually puts <see cref="ConstraintDefinitionSerializer"/> - and with it the upgrade of definitions
/// persisted before a unique event type constraint could cover several event types - on the read path.
/// </remarks>
public class ConstraintDefinitionSerializationProvider : IBsonSerializationProvider
{
    readonly ConstraintDefinitionSerializer _serializer = new();

    /// <inheritdoc/>
    public IBsonSerializer? GetSerializer(Type type) =>
        type == typeof(IConstraintDefinition) ? _serializer : null;
}
