// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Maps delivery outcomes to transport status codes.
/// </summary>
internal interface IDeliveryStatusCodes
{
    /// <summary>
    /// Gets the status code for an outcome.
    /// </summary>
    /// <param name="outcome">The outcome.</param>
    /// <returns>The status code.</returns>
    int For(DeliveryOutcome outcome);
}
