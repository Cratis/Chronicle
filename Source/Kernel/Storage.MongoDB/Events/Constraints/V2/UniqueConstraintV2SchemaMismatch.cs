// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;

/// <summary>
/// The exception that is thrown when v2 metadata or physical storage cannot prove its ownership and shape.
/// </summary>
public sealed class UniqueConstraintV2SchemaMismatch : Exception
{
    /// <summary>Initializes a new instance of the <see cref="UniqueConstraintV2SchemaMismatch"/> class.</summary>
    public UniqueConstraintV2SchemaMismatch() : base("Unique constraint v2 storage has incompatible or damaged schema.")
    {
    }
}
