// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Holds the name the Neo4j graph sink asks the connection resolution for.
/// </summary>
public static class Neo4jConnectionStorage
{
    /// <summary>
    /// The value of <c language="text">storage</c> in a connection request for a graph. A resolved connection string is a bolt URI, a
    /// resolved database names a Neo4j database and a resolved identity is the credential.
    /// </summary>
    public const string Name = "Neo4j";
}
