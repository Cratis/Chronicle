// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Unrelated test event that a summary subscribes to only to count occurrences. It happens to carry a
/// <see cref="Location"/> property with the same name as the summary's location, which must not overwrite
/// the summary's explicitly sourced location when that property is flagged <c>[NoAutoMap]</c>.
/// </summary>
/// <param name="Location">A location on the submitting candidate — deliberately named to collide.</param>
[EventType]
public record CandidateSubmitted(string Location);
