// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

/// <summary>
/// The SQL owner exceeds the supported 450 UTF-16 code units. No identifier is truncated or sent to SQL.
/// </summary>
public sealed class UniqueConstraintV2OwnerTooLong() : Exception("The v2 unique-constraint owner exceeds 450 UTF-16 code units.");
