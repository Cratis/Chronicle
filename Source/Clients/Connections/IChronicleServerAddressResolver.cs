// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Defines a system that resolves the <see cref="ChronicleServerAddress"/> entries to connect to
/// from a <see cref="ChronicleConnectionString"/>.
/// </summary>
public interface IChronicleServerAddressResolver
{
    /// <summary>
    /// Resolves the server addresses for a connection string. For DNS SRV based connection strings
    /// (chronicle+srv) this performs a DNS lookup, so it is expected to be called on every connect
    /// to pick up changes in the set of servers.
    /// </summary>
    /// <param name="connectionString">The <see cref="ChronicleConnectionString"/> to resolve for.</param>
    /// <returns>The resolved <see cref="ChronicleServerAddress"/> entries.</returns>
    Task<IReadOnlyList<ChronicleServerAddress>> Resolve(ChronicleConnectionString connectionString);
}
