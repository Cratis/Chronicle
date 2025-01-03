// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Contracts;

/// <summary>
/// Defines the contract for working with namespaces within an event store.
/// </summary>
[Service]
public interface INamespaces
{
    /// <summary>
    /// Ensure a namespace exists.
    /// </summary>
    /// <param name="command">The <see cref="Ensure"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Ensure(EnsureNamespace command);

    /// <summary>
    /// Gets all available namespaces in an event store.
    /// </summary>
    /// <param name="request">A <see cref="GetNamespacesRequest"/> instance.</param>
    /// <returns>Collection of strings representing the names of the namespaces.</returns>
    [Operation]
    Task<IEnumerable<string>> GetNamespaces(GetNamespacesRequest request);

    /// <summary>
    /// Observe all available namespaces in an event store.
    /// </summary>
    /// <param name="request">A <see cref="GetNamespacesRequest"/> instance.</param>
    /// <returns>An observable of collection of strings representing the names of the namespaces.</returns>
    [Operation]
    IObservable<IEnumerable<string>> ObserveNamespaces(GetNamespacesRequest request);
}
