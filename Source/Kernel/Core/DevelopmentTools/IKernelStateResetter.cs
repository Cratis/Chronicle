// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.DevelopmentTools;

/// <summary>
/// Defines something that can reset the kernel back to a freshly bootstrapped state.
/// </summary>
/// <remarks>
/// Exists so <see cref="ResetKernelState"/> can declare the public Handle() method Arc requires without the
/// implementation - which reaches into storage internals and the bootstrap handler - having to become public
/// surface along with it.
/// </remarks>
public interface IKernelStateResetter
{
    /// <summary>
    /// Resets the kernel state.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Reset();
}
