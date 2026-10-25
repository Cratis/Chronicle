// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Describes the outcome of receiving a delivery.
/// </summary>
internal enum DeliveryStatus
{
    /// <summary>
    /// The batch was handled successfully.
    /// </summary>
    Succeeded = 0,

    /// <summary>
    /// Dispatch failed.
    /// </summary>
    Failed = 1,

    /// <summary>
    /// Dispatch was cancelled or did not publish a result.
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// No active observer matches the target.
    /// </summary>
    UnknownObserver = 3,

    /// <summary>
    /// The body or target is invalid.
    /// </summary>
    Malformed = 4,

    /// <summary>
    /// The request exceeds the local body limit.
    /// </summary>
    TooLarge = 5
}
