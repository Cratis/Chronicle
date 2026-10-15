// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents Chronicle-specific MongoDB storage options.
/// </summary>
public class MongoDBStorageOptions
{
    /// <summary>
    /// Gets or sets the prefix prepended to every database name. Empty preserves the default names.
    /// </summary>
    public string DatabaseNamePrefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how the connections to the cluster, event store, namespace and read model databases are resolved.
    /// </summary>
    public ConnectionResolutionOptions ConnectionResolution { get; set; } = new();
}
