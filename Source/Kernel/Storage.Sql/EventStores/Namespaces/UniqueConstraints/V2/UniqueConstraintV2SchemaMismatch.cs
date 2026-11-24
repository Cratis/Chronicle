// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

/// <summary>
/// The persisted v2 binding cannot safely identify the requested logical storage.
/// </summary>
public sealed class UniqueConstraintV2SchemaMismatch() : Exception("The v2 unique-constraint storage binding does not match its logical identity.");
