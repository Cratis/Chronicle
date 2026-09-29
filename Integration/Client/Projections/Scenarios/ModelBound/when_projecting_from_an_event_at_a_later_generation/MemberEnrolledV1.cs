// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_from_an_event_at_a_later_generation;

/// <summary>
/// First generation of the event a member is enrolled with - the name as a single value.
/// </summary>
/// <param name="FullName">The member's full name.</param>
[EventType("MemberEnrolled", generation: 1)]
public record MemberEnrolledV1(string FullName);
