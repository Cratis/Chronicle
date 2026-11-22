// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;

/// <summary>Defines stable BSON identities, proofs and lossless sequence-number storage.</summary>
internal static class V2Documents
{
    /// <summary>The feature ownership marker, never inferred from a collection name.</summary>
    public const string Owner = "chronicle.unique-constraints.v2";

    /// <summary>Gets the feature-owned metadata marker.</summary>
    /// <param name="collection">The exact metadata collection.</param>
    /// <returns>The versioned ownership marker.</returns>
    public static BsonDocument Metadata(string collection) => new() { { "_id", "schema" }, { "Kind", Owner }, { "Version", 2 }, { "Physical", collection } };

    /// <summary>Gets an authority's complete logical identity.</summary>
    /// <param name="sequence">The event sequence.</param>
    /// <param name="constraint">The constraint.</param>
    /// <returns>The digest lookup and complete identity.</returns>
    public static BsonDocument Identity(string sequence, string constraint) => new()
    {
        { "_id", V2Identity.Digest(sequence, constraint) }, { "Kind", Owner }, { "Version", 2 }, { "Sequence", sequence }, { "Constraint", constraint }
    };

    /// <summary>Gets an exact conditional authority proof.</summary>
    /// <param name="identity">The complete logical identity.</param>
    /// <param name="authority">The expected public proof.</param>
    /// <returns>The complete filter.</returns>
    public static BsonDocument Proof(BsonDocument identity, UniqueConstraintWriteAuthority authority)
    {
        var proof = identity.DeepClone().AsBsonDocument;
        proof.Add("Epoch", authority.Epoch.Value.ToString("N"))
            .Add("Run", Run(authority.Phase))
            .Add("Revision", authority.Revision.Value);
        return proof;
    }

    /// <summary>Gets the BSON phase value, including explicit null for Live.</summary>
    /// <param name="phase">The phase.</param>
    /// <returns>The exact run identifier or BSON null.</returns>
    public static BsonValue Run(UniqueConstraintWritePhase phase) => phase.RunId is null ? BsonNull.Value : new BsonString(phase.RunId.Value.ToString("N"));

    /// <summary>Validates full logical identity and decodes the public proof.</summary>
    /// <param name="document">The persisted authority.</param>
    /// <param name="identity">The expected complete identity.</param>
    /// <returns>The persisted proof.</returns>
    /// <exception cref="UniqueConstraintV2SchemaMismatch">The identity or proof is damaged.</exception>
    public static UniqueConstraintWriteAuthority Authority(BsonDocument document, BsonDocument identity)
    {
        if (identity.Any(element => !document.TryGetValue(element.Name, out var value) || value != element.Value)) throw new UniqueConstraintV2SchemaMismatch();
        var epoch = Identifier(document.GetValue("Epoch", BsonNull.Value));
        _ = Identifier(document.GetValue("Fence", BsonNull.Value));
        var phase = Phase(document.GetValue("Run", "invalid"));
        var revision = Revision(document.GetValue("Revision", BsonNull.Value));
        return new(new(epoch), phase, revision);
    }

    /// <summary>Encodes the full unsigned range without an Int64 narrowing conversion.</summary>
    /// <param name="sequence">The original sequence number.</param>
    /// <returns>The exact BSON decimal.</returns>
    public static BsonDecimal128 Sequence(EventSequenceNumber sequence) => new((decimal)sequence.Value);

    /// <summary>Decodes an integral unsigned sequence without rounding.</summary>
    /// <param name="value">The persisted decimal.</param>
    /// <returns>The exact sequence number.</returns>
    /// <exception cref="UniqueConstraintV2SchemaMismatch">The sequence is not an exact unsigned integer.</exception>
    public static EventSequenceNumber Sequence(BsonValue value)
    {
        if (!value.IsDecimal128) throw new UniqueConstraintV2SchemaMismatch();
        try
        {
            var number = Decimal128.ToDecimal(value.AsDecimal128);
            if (value.AsDecimal128 != new Decimal128(number)) throw new UniqueConstraintV2SchemaMismatch();
            if (number < 0) throw new UniqueConstraintV2SchemaMismatch();
            if (number > ulong.MaxValue) throw new UniqueConstraintV2SchemaMismatch();
            if (decimal.Truncate(number) != number) throw new UniqueConstraintV2SchemaMismatch();
            return new((ulong)number);
        }
        catch (OverflowException)
        {
            throw new UniqueConstraintV2SchemaMismatch();
        }
    }

    static Guid Identifier(BsonValue value)
    {
        if (!value.IsString) throw new UniqueConstraintV2SchemaMismatch();
        if (!Guid.TryParseExact(value.AsString, "N", out var identifier)) throw new UniqueConstraintV2SchemaMismatch();
        if (identifier == Guid.Empty) throw new UniqueConstraintV2SchemaMismatch();
        return identifier;
    }

    static UniqueConstraintAuthorityRevision Revision(BsonValue value)
    {
        if (value is not BsonInt64 { Value: >= 0 } revision) throw new UniqueConstraintV2SchemaMismatch();
        return new(revision.Value);
    }

    static UniqueConstraintWritePhase Phase(BsonValue run) => run.IsBsonNull ? UniqueConstraintWritePhase.Live : UniqueConstraintWritePhase.Run(new(Identifier(run)));
}
