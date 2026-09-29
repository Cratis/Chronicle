// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_from_an_event_at_a_later_generation;

/// <summary>
/// Second generation of the event a member is enrolled with - the name split into its parts.
/// </summary>
/// <param name="FirstName">The member's first name.</param>
/// <param name="LastName">The member's last name.</param>
[EventType(generation: 2)]
public record MemberEnrolled(string FirstName, string LastName);
