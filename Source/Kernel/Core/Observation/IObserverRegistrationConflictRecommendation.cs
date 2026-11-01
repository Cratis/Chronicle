// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Recommendations;
using Orleans.CodeGeneration;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a recommendation for connected-client registrations on conflicting sequences.
/// </summary>
[Version(1)]
public interface IObserverRegistrationConflictRecommendation : IRecommendation<ObserverRegistrationConflictRequest>;
