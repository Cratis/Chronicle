// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
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
/// <param name="reactors">The kernel reactors registry.</param>
/// <param name="authenticationService">The authentication bootstrap service.</param>
[Singleton]
internal sealed class KernelBootstrapResetHandler(
    IGrainFactory grainFactory,
    IReactors reactors,
    IAuthenticationService authenticationService)
{
    /// <summary>
    /// Re-bootstrap the system event store, kernel reactors, and default identity data.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public async Task Bootstrap()
    {
        await grainFactory.GetGrain<INamespaces>(EventStoreName.System).EnsureDefault();
        await reactors.DiscoverAndRegister(EventStoreName.System, EventStoreNamespaceName.Default);
        await authenticationService.EnsureDefaultAdminUser();
        await authenticationService.EnsureBootstrapClients();
#if DEVELOPMENT
        await authenticationService.EnsureDefaultClientCredentials();
#endif
    }
}
