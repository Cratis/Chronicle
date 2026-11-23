// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;

/// <summary>
/// The exception that is thrown when MongoDB cannot positively confirm replica-set transaction support.
/// </summary>
public sealed class UniqueConstraintV2TransactionsRequired : Exception
{
    /// <summary>Initializes a new instance of the <see cref="UniqueConstraintV2TransactionsRequired"/> class.</summary>
    public UniqueConstraintV2TransactionsRequired() : base("Unique constraint v2 storage requires a confirmed transaction-capable MongoDB replica set.")
    {
    }
}
