// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The runtime source of truth for federation bindings and grants. State is folded from the federation events in the
/// system event log, which is also the audit trail, so every silo sees the same state. Runtime state wins over configuration.
/// </summary>
public interface IFederationRegistry
{
    /// <summary>
    /// Get the effective consumer bindings.
    /// </summary>
    /// <returns>The bindings.</returns>
    Task<IReadOnlyList<FederationBindingState>> GetBindings();

    /// <summary>
    /// Get the effective producer grants, revoked ones included.
    /// </summary>
    /// <returns>The grants.</returns>
    Task<IReadOnlyList<FederationGrantState>> GetGrants();

    /// <summary>
    /// Find the active (not revoked) grants whose pinned client certificate matches.
    /// </summary>
    /// <param name="pin">The pin of the presented client certificate.</param>
    /// <returns>The active grants for the pin, none when unknown or revoked.</returns>
    Task<IReadOnlyList<FederationGrantState>> FindActiveGrants(CertificatePin pin);

    /// <summary>
    /// Create or change a consumer binding.
    /// </summary>
    /// <param name="binding">The binding, with <see cref="FederationOrigin.Runtime"/> origin.</param>
    /// <returns>Awaitable task.</returns>
    Task Bind(FederationBindingState binding);

    /// <summary>
    /// Remove a consumer binding.
    /// </summary>
    /// <param name="id">The binding id.</param>
    /// <returns>Awaitable task.</returns>
    Task Unbind(string id);

    /// <summary>
    /// Create or change a producer grant, reinstating a revoked link.
    /// </summary>
    /// <param name="grant">The grant, with <see cref="FederationOrigin.Runtime"/> origin.</param>
    /// <returns>Awaitable task.</returns>
    Task SetGrant(FederationGrantState grant);

    /// <summary>
    /// Revoke a producer link.
    /// </summary>
    /// <param name="id">The link id.</param>
    /// <returns>Awaitable task.</returns>
    Task Revoke(string id);

    /// <summary>
    /// Append events for configured bindings and grants that have no runtime history yet. Never overwrites or resurrects runtime state.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task BootstrapFromConfiguration();
}
