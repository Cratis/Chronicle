// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event observed only for a counter, but which also happens to carry a <see cref="Location"/>
/// property whose name collides with the read model's location — used to verify that AutoMap does not
/// bleed this value over an explicitly-sourced property.
/// </summary>
/// <param name="Name">The candidate name.</param>
/// <param name="Location">The candidate's location (a name collision, not the read model's source).</param>
[EventType]
public record CandidateSubmitted(string Name, string Location);
