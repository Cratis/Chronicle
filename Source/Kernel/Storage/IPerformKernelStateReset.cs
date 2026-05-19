// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Defines an extension point invoked from <c>IServer.ResetKernelState</c> in development builds.
/// Storage providers implement this to release transient resources (connection pools, caches)
/// when the kernel is being recycled between integration test specs without a container restart.
/// </summary>
public interface IPerformKernelStateReset
{
    /// <summary>
    /// Reset any transient state held by this component.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Reset();
}
