// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;

/// <summary>
/// Everything the kernel holds for an observer in one namespace.
/// </summary>
/// <param name="HasDefinition">Whether the event store has a definition of the observer.</param>
/// <param name="HasState">Whether the namespace has a state record for the observer.</param>
/// <param name="FailedPartitions">How many failed partitions the observer has.</param>
/// <param name="Jobs">How many jobs the observer has, finished or not.</param>
/// <param name="HasDurableSubscription">Whether the durable subscription of the observer is recorded.</param>
/// <param name="Reminders">How many reminders the observer's grain has.</param>
/// <param name="IsListed">Whether the observer is in the listing of observers.</param>
public record Footprint(bool HasDefinition, bool HasState, int FailedPartitions, int Jobs, bool HasDurableSubscription, int Reminders, bool IsListed)
{
    /// <summary>
    /// Gets a footprint of nothing at all.
    /// </summary>
    public static readonly Footprint None = new(false, false, 0, 0, false, 0, false);
}
