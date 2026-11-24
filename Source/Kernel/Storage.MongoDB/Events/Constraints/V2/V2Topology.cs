// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;

/// <summary>Positively establishes the replica-set capability required by the dormant provider.</summary>
internal static class V2Topology
{
    /// <summary>Checks the primary before any mutation or provisioning.</summary>
    /// <param name="database">The explicitly selected namespace database.</param>
    /// <param name="cancellationToken">Cancellation of the check.</param>
    /// <returns>The capability check.</returns>
    /// <exception cref="UniqueConstraintV2TransactionsRequired">The topology is unsupported or unconfirmed.</exception>
    public static async Task Require(IMongoDatabase database, CancellationToken cancellationToken)
    {
        var hello = await database.RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1), ReadPreference.Primary, cancellationToken);
        if (!SupportsTransactions(hello)) throw new UniqueConstraintV2TransactionsRequired();
    }

    /// <summary>Classifies hello without treating unknown topology as supported.</summary>
    /// <param name="hello">The primary hello reply.</param>
    /// <returns>Whether the server confirms the required replica-set capabilities.</returns>
    public static bool SupportsTransactions(BsonDocument hello) =>
        hello.GetValue("setName", BsonNull.Value) is BsonString set && set.Value.Length > 0 &&
        hello.GetValue("isWritablePrimary", false) == BsonBoolean.True &&
        hello.GetValue("logicalSessionTimeoutMinutes", BsonNull.Value) is BsonInt32 { Value: > 0 } &&
        hello.GetValue("maxWireVersion", BsonNull.Value) is BsonInt32 { Value: >= 9 } &&
        hello.GetValue("msg", string.Empty) != "isdbgrid";
}
