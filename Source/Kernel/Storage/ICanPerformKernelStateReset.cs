// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Defines an extension point invoked from <c>IServer.ResetKernelState</c> in development builds.
/// Storage providers implement this to wipe their backing store between integration test specs
/// without restarting the container — the kernel deactivates its grains, each provider clears
/// only its own data, and the kernel then re-bootstraps the system event store and identity data.
/// </summary>
public interface ICanPerformKernelStateReset
{
    /// <summary>
    /// Indicates whether this handler is responsible for resetting the currently configured
    /// storage backend. Implementations typically check <c>ChronicleOptions.Storage.Type</c>.
    /// Handlers whose <c>CanReset</c> returns <see langword="false"/> are skipped.
    /// </summary>
    /// <returns><see langword="true"/> when this handler should perform a reset, otherwise <see langword="false"/>.</returns>
    bool CanReset();

    /// <summary>
    /// Reset the backing storage and any transient state owned by this component.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Reset();
}
