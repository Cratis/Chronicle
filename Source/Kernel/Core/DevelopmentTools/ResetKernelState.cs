// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Chronicle.DevelopmentTools;

/// <summary>
/// Represents the command for resetting the kernel back to a freshly bootstrapped state.
/// </summary>
[Command]
public record ResetKernelState
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="resetter">The <see cref="IKernelStateResetter"/> performing the reset.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IKernelStateResetter resetter) => resetter.Reset();
}
#endif
