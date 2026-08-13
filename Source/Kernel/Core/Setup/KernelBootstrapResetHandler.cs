// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Setup.Authentication;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Re-runs the parts of <see cref="ChronicleServerStartupTask"/> that recreate data the next
/// integration spec relies on after the storage reset wipes the underlying databases.
/// Invoked by <c>IServer.ResetKernelState</c> after all <c>ICanPerformKernelStateReset</c>
/// handlers have wiped their backing stores — never before, because <c>EnsureDefault*</c>
/// methods are no-ops when their target data already exists.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> used to look up grains.</param>
/// <param name="eventTypes">The kernel event types registry.</param>
/// <param name="reactors">The kernel reactors registry.</param>
/// <param name="authenticationService">The authentication bootstrap service.</param>
[Singleton]
internal sealed class KernelBootstrapResetHandler(
    IGrainFactory grainFactory,
    IEventTypes eventTypes,
    IReactors reactors,
    IAuthenticationService authenticationService)
{
    /// <summary>
    /// Re-bootstrap the system event store, its event types, kernel reactors, and default identity data.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public async Task Bootstrap()
    {
        await grainFactory.GetGrain<INamespaces>(EventStoreName.System).EnsureDefault();
        await reactors.DiscoverAndRegister(EventStoreName.System, EventStoreNamespaceName.Default);

        // The System event store's event type schemas live in its own database, which the reset drops.
        // Every other store re-registers them through EventStores.Ensure when a client reconnects, but
        // nothing re-registers the System store's — so without this the kernel's own events (e.g.
        // ApplicationAuthenticated) fail to append with MissingEventSchemaForEventType for the rest of
        // the kernel's lifetime.
        await eventTypes.DiscoverAndRegister(EventStoreName.System);

        await authenticationService.EnsureDefaultAdminUser();
        await authenticationService.EnsureBootstrapClients();
#if DEVELOPMENT
        await authenticationService.EnsureDefaultClientCredentials();
#endif
    }
}
