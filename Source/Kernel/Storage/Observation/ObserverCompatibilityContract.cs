// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents the fixed subscriber and filters of a namespace observer.
/// </summary>
/// <param name="SubscriberType">The assembly-qualified subscriber type name.</param>
/// <param name="Filters">The registration's filters.</param>
public record ObserverCompatibilityContract(string SubscriberType, ObserverFilters Filters);
