// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents what kind of thing went wrong on an attempt at a partition.
/// </summary>
/// <remarks>
/// A failed partition used to carry only the message it failed with, which made an observer that is genuinely wrong
/// look exactly like a kernel that was too busy to answer in time. They call for opposite responses - one wants the
/// code fixed, the other wants to be left alone until the queue drains - and only one of them should count toward
/// quarantining an observer.
/// </remarks>
public enum FailureKind
{
    /// <summary>
    /// Nothing classified the failure. Every attempt recorded before failures carried a kind reads back as this.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The subscriber failed while handling the events. This is the failure that means something is wrong.
    /// </summary>
    Handling = 1,

    /// <summary>
    /// The call to the subscriber did not come back in time. The events were never rejected - the kernel ran out of
    /// patience waiting for an answer, which says the system was congested rather than that the observer is wrong.
    /// </summary>
    Timeout = 2,

    /// <summary>
    /// The subscriber was gone by the time the events reached it.
    /// </summary>
    Disconnected = 3
}
