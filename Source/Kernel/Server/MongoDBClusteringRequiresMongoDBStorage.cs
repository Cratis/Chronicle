// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

/// <summary>
/// The exception that is thrown when cluster membership cannot use the configured storage backend.
/// </summary>
/// <param name="storage">The configured storage type.</param>
public sealed class MongoDBClusteringRequiresMongoDBStorage(string storage)
    : Exception($"Clustering.Type 'MongoDB' requires Storage.Type 'mongodb'. Configured Storage.Type: '{storage}'.");
