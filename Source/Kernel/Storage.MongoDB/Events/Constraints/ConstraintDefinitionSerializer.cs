// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Strings;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents a serializer for <see cref="IConstraintDefinition"/>.
/// </summary>
/// <remarks>
/// The serializer exists for one reason: to upgrade a definition persisted by an older kernel on the way in. It
/// therefore writes exactly what the driver's own discriminated-interface path writes - the concrete type's
/// document with its <c>_t</c> discriminator - so that putting it on the read path migrates nothing and leaves
/// every stored document readable by a kernel that does not have it.
/// <para>
/// It is put on that path by <see cref="ConstraintDefinitionSerializationProvider"/>. Registering it as a bare
/// <see cref="IBsonSerializer"/> does not work: the driver resolves every interface to a
/// <c>DiscriminatedInterfaceSerializer</c> of its own, so the auto-registration in
/// <see cref="Serialization.CustomSerializers"/> finds one already there and skips this one.
/// </para>
/// </remarks>
public class ConstraintDefinitionSerializer : SerializerBase<IConstraintDefinition>, IBsonDocumentSerializer
{
    /// <summary>
    /// The element the driver's discriminated-interface path names the concrete type with.
    /// </summary>
    const string DiscriminatorElementName = "_t";

    /// <summary>
    /// The element an earlier revision of this serializer named the concrete type with.
    /// </summary>
    /// <remarks>
    /// No shipped kernel ever wrote it - this serializer was never on the write path - so this is tolerance for a
    /// store written by a build that did register it, not a format anything produces.
    /// </remarks>
    const string ConstraintTypeElementName = "constraintType";

    /// <summary>
    /// The element a unique event type constraint definition was persisted with before it covered several event types.
    /// </summary>
    const string LegacyUniqueEventTypeElementName = "eventTypeId";

    static readonly string _uniqueEventTypesElementName = nameof(UniqueEventTypeConstraintDefinition.EventTypeIds).ToCamelCase();

    /// <inheritdoc/>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IConstraintDefinition value)
    {
        var type = value.GetType();
        var actualSerializer = BsonSerializer.SerializerRegistry.GetSerializer(type);
        actualSerializer.Serialize(context, new BsonSerializationArgs { NominalType = type }, value);
    }

    /// <inheritdoc/>
    public override IConstraintDefinition Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var rawBsonDocument = context.Reader.ReadRawBsonDocument();
        using var rawDocument = new RawBsonDocument(rawBsonDocument);
        var bsonDocument = rawDocument.ToBsonDocument<BsonDocument>();
        var type = GetConstraintDefinitionType(bsonDocument);

        if (type == typeof(UniqueEventTypeConstraintDefinition))
        {
            bsonDocument = UpgradeLegacyUniqueEventTypeDefinition(bsonDocument);
        }

        return (IConstraintDefinition)BsonSerializer.Deserialize(bsonDocument, type);
    }

    /// <inheritdoc/>
    public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
    {
        serializationInfo = null!;
        return false;
    }

    /// <summary>
    /// Resolve the concrete definition type a stored document holds.
    /// </summary>
    /// <param name="document">The <see cref="BsonDocument"/> that was read.</param>
    /// <returns>The concrete <see cref="Type"/> to deserialize as.</returns>
    /// <exception cref="UnknownConstraintTypeString">Thrown when the document names no type this serializer recognizes.</exception>
    static Type GetConstraintDefinitionType(BsonDocument document)
    {
        if (document.TryGetValue(DiscriminatorElementName, out var discriminator))
        {
            return BsonSerializer.LookupActualType(typeof(IConstraintDefinition), discriminator);
        }

        if (!document.TryGetValue(ConstraintTypeElementName, out var constraintType))
        {
            throw new UnknownConstraintTypeString($"<no '{DiscriminatorElementName}' or '{ConstraintTypeElementName}' element>");
        }

        var constraintTypeString = constraintType.AsString;
        if (!Enum.TryParse<ConstraintType>(constraintTypeString, out var parsed))
        {
            throw new UnknownConstraintTypeString(constraintTypeString);
        }

        return parsed switch
        {
            ConstraintType.Unique => typeof(UniqueConstraintDefinition),
            ConstraintType.UniqueEventType => typeof(UniqueEventTypeConstraintDefinition),
            _ => throw new UnknownConstraintTypeString(constraintTypeString)
        };
    }

    /// <summary>
    /// Upgrade a unique event type constraint definition persisted before the constraint could cover several event types.
    /// </summary>
    /// <param name="document">The <see cref="BsonDocument"/> that was read.</param>
    /// <returns>The document to deserialize from - the one that was read when there is nothing to upgrade.</returns>
    /// <remarks>
    /// The definition used to carry a single event type. Deserializing that document into the current record leaves the
    /// covered event types absent, which every reader then dereferences — registration compares the stored definition
    /// with the incoming one and dies before a single constraint is registered. Mapping the single event type onto a
    /// one-element sequence keeps the constraint's meaning across the upgrade; the next registration persists the new shape.
    /// <para>
    /// The upgrade produces a new document rather than editing the one that was read, which is a
    /// <see cref="RawBsonDocument"/> over the bytes off the wire and rejects every mutation.
    /// </para>
    /// </remarks>
    static BsonDocument UpgradeLegacyUniqueEventTypeDefinition(BsonDocument document)
    {
        if (document.Contains(_uniqueEventTypesElementName) ||
            !document.TryGetValue(LegacyUniqueEventTypeElementName, out var legacyEventType))
        {
            return document;
        }

        var upgraded = new BsonDocument(document.Elements.Where(element => element.Name != LegacyUniqueEventTypeElementName));
        upgraded.Add(_uniqueEventTypesElementName, new BsonArray { legacyEventType });
        return upgraded;
    }
}
