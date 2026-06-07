// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Dictionary-backed entity used for read-model rows whose shape is only known at runtime
/// (one entity registration per read model, with columns derived from the read model's
/// <see cref="Schemas.JsonSchema"/>). Each entry corresponds to one column;
/// EF treats it as a shared-type entity with indexer-properties, so per-column dirty tracking
/// gives us field-level UPDATEs without needing a generated CLR type per read model.
/// </summary>
public class DynamicReadModelEntity : Dictionary<string, object?>;
