// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// The classified index definitions and unclassified constraint collections within a namespace.
/// </summary>
/// <param name="Definitions">The indexes safe to verify or provision.</param>
/// <param name="Unclassified">Existing constraint collection names whose definitions do not select one mode.</param>
internal record MongoDBNamespaceIndexes(IReadOnlyList<MongoDBIndexDefinition> Definitions, IReadOnlyList<string> Unclassified);
