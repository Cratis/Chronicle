// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Recommendations;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a recommendation for registrations on conflicting event sequences.
/// </summary>
public class ObserverRegistrationConflictRecommendation : Recommendation<ObserverRegistrationConflictRequest>, IObserverRegistrationConflictRecommendation;
