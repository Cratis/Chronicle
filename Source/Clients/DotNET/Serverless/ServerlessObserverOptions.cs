// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Represents the serverless settings of a single observer.
/// </summary>
public class ServerlessObserverOptions
{
    /// <summary>
    /// Gets or sets the delivery for the observer. When set it takes precedence over <see cref="DeliveryAttribute"/>.
    /// </summary>
    public ObserverDelivery? Delivery { get; set; }
}
