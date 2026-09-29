// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_from_an_event_at_a_later_generation;

/// <summary>
/// Model-bound read model populated only by AutoMap from the second generation of <see cref="MemberEnrolled"/>.
/// </summary>
/// <param name="Id">The member identifier.</param>
/// <param name="FirstName">The member's first name.</param>
/// <param name="LastName">The member's last name.</param>
[FromEvent<MemberEnrolled>]
public record EnrolledMember(Guid Id, string FirstName, string LastName);
