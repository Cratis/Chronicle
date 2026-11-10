// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Neo4j.Integration;

/// <summary>
/// Base class for Neo4j integration specs that use a typed context, the way <c language="text">MongoDBGiven</c> does for the MongoDB suite.
/// </summary>
/// <typeparam name="TSetup">Type of the context.</typeparam>
/// <param name="context">The context to use.</param>
public class Neo4jGiven<TSetup>(TSetup context) : IClassFixture<TSetup>
    where TSetup : class
{
    /// <summary>
    /// Gets the context for the current specification.
    /// </summary>
    public TSetup Context { get; } = context;
}
