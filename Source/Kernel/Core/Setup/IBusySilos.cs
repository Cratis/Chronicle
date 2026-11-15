// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Defines a system that tells a busy silo apart from one that cannot be reached.
/// </summary>
/// <remarks>
/// A grain call that times out is ambiguous on its own. When it was addressed to the silo making it, or to a sibling
/// the cluster still reports as active, the target is there and working through a backlog - waiting for it is the
/// remedy, and restarting the caller only adds to the work. When it was addressed to a silo the cluster no longer
/// reports as active, or the target is unknown, it may be a silo that is gone or a cluster that is still forming.
/// </remarks>
public interface IBusySilos
{
    /// <summary>
    /// Finds the busy silo a failed call was waiting on.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> the call failed with.</param>
    /// <returns>The <see cref="BusySilo"/> when the call timed out against a silo that is up, otherwise <see langword="null"/>.</returns>
    BusySilo? FindBusySilo(Exception exception);
}
