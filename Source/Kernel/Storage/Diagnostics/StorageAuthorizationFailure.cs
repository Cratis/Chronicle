// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Diagnostics;

/// <summary>
/// Identifies a storage operation refused by the storage provider.
/// </summary>
/// <param name="Database">The database name.</param>
/// <param name="Collection">The collection name.</param>
/// <param name="Operation">The refused operation name.</param>
public record StorageAuthorizationFailure(string Database, string Collection, string Operation);
