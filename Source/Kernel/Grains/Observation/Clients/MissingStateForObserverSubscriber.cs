// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Observation;

namespace Cratis.Kernel.Grains.Observation.Clients;

/// <summary>
/// Exception that gets thrown when there is no state for an observer subscriber.
/// </summary>
public class MissingStateForObserverSubscriber : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingStateForObserverSubscriber"/> class.
    /// </summary>
    /// <param name="observerId"><see cref="ObserverId"/> that has missing state.</param>
    public MissingStateForObserverSubscriber(ObserverId observerId)
        : base($"Missing state for observer subscriber with id {observerId}")
    {
    }
}
