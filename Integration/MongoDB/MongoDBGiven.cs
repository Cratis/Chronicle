// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.MongoDB.Integration;

/// <summary>
/// Base class for MongoDB integration specs that use a typed context.
/// Equivalent to <see cref="Given{TSetup}"/> but without
/// the <c>IChronicleSetupFixture</c> constraint so that lightweight MongoDB-only contexts
/// can be used without starting an Orleans silo.
/// </summary>
/// <typeparam name="TSetup">Type of the context.</typeparam>
public class MongoDBGiven<TSetup> : IClassFixture<TSetup>
    where TSetup : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBGiven{TSetup}"/> class.
    /// </summary>
    /// <param name="context">The context to use.</param>
    public MongoDBGiven(TSetup context)
    {
        Context = context;
        if (context is MongoDBSpecification spec)
        {
            spec.SetName(GetType().FullName!);
        }
    }

    /// <summary>
    /// Gets the context for the current specification.
    /// </summary>
    public TSetup Context { get; }
}
