// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Clients;

/// <summary>
/// Exception that gets thrown when there is no state for an observer subscriber.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingStateForReactorSubscriber"/> class.
/// </remarks>
/// <param name="observerId"><see cref="ObserverId"/> that has missing state.</param>
public class MissingStateForReactorSubscriber(ObserverId observerId) : Exception($"Missing state for Reactor observer subscriber with id {observerId}")
{
}
