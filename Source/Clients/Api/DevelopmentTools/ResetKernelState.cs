// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Contracts.Host;

namespace Cratis.Chronicle.Api.DevelopmentTools;

/// <summary>
/// Represents the command for wiping every event store and re-bootstrapping the kernel.
/// </summary>
/// <remarks>
/// Only compiled into development builds. A production image has no such endpoint at all, rather
/// than one that exists and refuses - there is nothing here to reach.
/// </remarks>
[Command]
public record ResetKernelState
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="server">The <see cref="IServer"/> contract.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IServer server) => server.ResetKernelState();
}
#endif
