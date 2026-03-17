// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Defines the initializer responsible for bootstrapping Chronicle artifacts (event types, namespaces, reactors, projections, etc.).
/// </summary>
/// <remarks>
/// Separated from <see cref="ChronicleServerStartupTask"/> so the same logic can be invoked
/// after a development-tools reset without restarting the silo.
/// </remarks>
public interface IChronicleInitializer
{
    /// <summary>
    /// Initialize (or re-initialize) the full Chronicle system.
    /// </summary>
    /// <remarks>
    /// Discovers all registered event stores and re-bootstraps namespaces, event types,
    /// managers (projections, read models, webhooks), reactor registrations, and the default
    /// admin user. Job rehydration is intentionally skipped so callers can opt in separately.
    /// </remarks>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Initialize(CancellationToken cancellationToken = default);
}
