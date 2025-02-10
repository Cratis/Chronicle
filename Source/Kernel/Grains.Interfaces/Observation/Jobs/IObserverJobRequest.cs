// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Defines the basis for a request to an observer.
/// </summary>
public interface IObserverJobRequest : IJobRequest
{
    /// <summary>
    /// Gets the <see cref="ObserverKey"/> for the request.
    /// </summary>
    ObserverKey ObserverKey { get; }
}
