// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Recommendations;

namespace Cratis.Chronicle.Observation.States;

/// <summary>
/// Defines the Grain that represents a replay candidate for an observer.
/// </summary>
public interface IReplayCandidateRecommendation : IRecommendation<ReplayCandidateRequest>;
