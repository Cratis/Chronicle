// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInjection;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Represents an implementation of <see cref="INeo4jDriverFactory"/>.
/// </summary>
[Singleton]
public class Neo4jDriverFactory : INeo4jDriverFactory, IAsyncDisposable
{
    readonly ConcurrentDictionary<(string Server, IAuthTokenManager Authentication), IDriver> _drivers = new();
    readonly Func<Uri, IAuthTokenManager, IDriver> _createDriver;

    /// <summary>
    /// Initializes a new instance of the <see cref="Neo4jDriverFactory"/> class.
    /// </summary>
    public Neo4jDriverFactory()
        : this(static (server, authentication) => GraphDatabase.Driver(server, authentication))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Neo4jDriverFactory"/> class with a native driver constructor.
    /// </summary>
    /// <param name="createDriver">The native driver constructor.</param>
    internal Neo4jDriverFactory(Func<Uri, IAuthTokenManager, IDriver> createDriver) => _createDriver = createDriver;

    /// <summary>
    /// Gets the number of drivers created.
    /// </summary>
    internal int Count => _drivers.Count;

    /// <inheritdoc/>
    public IDriver Create(Uri server, IAuthTokenManager authentication) =>
        _drivers.GetOrAdd(
            (server.ToString(), authentication),
            static (_, state) => state.Create(state.Server, state.Authentication),
            (Create: _createDriver, Server: server, Authentication: authentication));

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        var drivers = _drivers.Values.ToArray();
        _drivers.Clear();
        foreach (var driver in drivers)
        {
            await driver.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
