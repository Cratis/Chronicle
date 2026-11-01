// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the outcome of handing an observer's stored progress over to another event sequence in one namespace.
/// </summary>
public enum SequenceHandoverResult
{
    /// <summary>
    /// There was no progress to hand over; ownership was only claimed.
    /// </summary>
    Claimed = 0,

    /// <summary>
    /// The stored progress already belongs to the target sequence.
    /// </summary>
    AlreadyOnTarget = 1,

    /// <summary>
    /// The target sequence's progress was selected.
    /// </summary>
    HandedOver = 2,

    /// <summary>
    /// The stored owner kept changing, so nothing was handed over.
    /// </summary>
    NotHandedOver = 3
}
