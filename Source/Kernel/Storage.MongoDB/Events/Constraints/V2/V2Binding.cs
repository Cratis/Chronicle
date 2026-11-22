// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;

/// <summary>
/// Represents the complete logical and physical ownership of one v2 claim collection.
/// </summary>
/// <param name="Sequence">The exact event sequence.</param>
/// <param name="Constraint">The exact constraint.</param>
/// <param name="Mode">The claim mode.</param>
internal sealed record V2Binding(string Sequence, string Constraint, UniqueConstraintMode Mode)
{
    /// <summary>Gets the logical lookup key.</summary>
    public string Id => V2Identity.Digest(Sequence, Constraint, Mode.ToString());

    /// <summary>Gets the bounded, v1-disjoint physical name.</summary>
    public string Physical => $"uc+{Id[..48]}+{(Mode == UniqueConstraintMode.PerValue ? "uc2v" : "uc2")}";

    /// <summary>Gets the complete persisted binding, not just its lookup digest.</summary>
    public BsonDocument Document => new()
    {
        { "_id", Id }, { "Kind", V2Documents.Owner }, { "Version", 2 },
        { "Sequence", Sequence }, { "Constraint", Constraint }, { "Mode", Mode.ToString() }, { "Physical", Physical }
    };
}
