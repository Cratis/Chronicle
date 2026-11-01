// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// The claimants of a placement: those proved from metadata, and scopes that could not be read.
/// </summary>
/// <param name="Known">The claims read from metadata and the ledger.</param>
/// <param name="Unknown">Scopes that are possible owners of every name in the placement.</param>
internal sealed record ReadModelTableClaims(
    IReadOnlyList<ReadModelTableClaim> Known,
    IReadOnlyList<ReadModelTableMetadataScope> Unknown);
