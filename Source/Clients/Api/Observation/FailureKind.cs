// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Observation;

/// <summary>
/// Defines what kind of thing went wrong on an attempt at a partition.
/// </summary>
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
    /// The call to the subscriber did not come back in time, which says the system was congested rather than that the
    /// observer is wrong.
    /// </summary>
    Timeout = 2,

    /// <summary>
    /// The subscriber was gone by the time the events reached it.
    /// </summary>
    Disconnected = 3
}
