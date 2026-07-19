// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the types of cluster membership the Chronicle server supports.
/// </summary>
public enum ClusteringType
{
    /// <summary>
    /// Single node development clustering on localhost.
    /// </summary>
    Localhost = 0,

    /// <summary>
    /// Multi node clustering with membership kept in MongoDB - all nodes sharing the same
    /// MongoDB storage and cluster id form one cluster.
    /// </summary>
    MongoDB = 1
}
