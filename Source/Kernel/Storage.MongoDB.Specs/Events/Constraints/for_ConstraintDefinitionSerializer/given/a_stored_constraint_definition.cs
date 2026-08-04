// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ConstraintDefinitionSerializer.given;

/// <summary>
/// Reads and writes a constraint definition the way the constraints collection does - as the
/// <see cref="StoredConstraintDefinition.Definition"/> member of a stored entry, resolved through
/// <see cref="BsonSerializer"/>.
/// </summary>
/// <remarks>
/// Going through the registry rather than a hand-constructed serializer is the point of this fixture. A spec that
/// news up the serializer and calls it directly passes whether or not anything ever puts it on the read path, which
/// is how a shipped upgrade of exactly these documents came to be covered by passing specs and never run once.
/// </remarks>
public class a_stored_constraint_definition : Specification
{
    protected const string ConstraintNameValue = "some-constraint";

    /// <summary>
    /// Read a definition document the way the constraints collection reads one.
    /// </summary>
    /// <param name="definition">The stored definition sub-document.</param>
    /// <returns>The <see cref="IConstraintDefinition"/> that was read.</returns>
    protected static IConstraintDefinition Read(BsonDocument definition) =>
        BsonSerializer.Deserialize<StoredConstraintDefinition>(new BsonDocument
        {
            { "_id", $"{ConstraintNameValue}-v1" },
            { "name", ConstraintNameValue },
            { "version", 1L },
            { "definition", definition }
        }).Definition;

    /// <summary>
    /// Write a definition the way the constraints collection writes one.
    /// </summary>
    /// <param name="definition">The definition to write.</param>
    /// <returns>The stored definition sub-document.</returns>
    protected static BsonDocument Write(IConstraintDefinition definition) =>
        new StoredConstraintDefinition($"{ConstraintNameValue}-v1", ConstraintNameValue, 1, definition)
            .ToBsonDocument()["definition"].AsBsonDocument;

    /// <summary>
    /// Build the stored shape of a definition persisted before a unique event type constraint covered several event types.
    /// </summary>
    /// <param name="eventTypeId">The single event type the constraint covered.</param>
    /// <returns>The legacy sub-document.</returns>
    protected static BsonDocument LegacyUniqueEventTypeDocument(string eventTypeId) => new()
    {
        { "_t", nameof(UniqueEventTypeConstraintDefinition) },
        { "_id", ConstraintNameValue },
        { "eventTypeId", eventTypeId }
    };
}
