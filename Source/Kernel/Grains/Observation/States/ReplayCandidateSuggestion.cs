// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Suggestions;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents an implementation of <see cref="IReplayCandidateSuggestion"/>.
/// </summary>
public class ReplayCandidateSuggestion : Suggestion<ReplayCandidateRequest>, IReplayCandidateSuggestion
{
}
