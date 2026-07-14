// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event enrolling a member into a group; lives on the group's event source and creates the child row.
/// </summary>
/// <param name="MemberId">The enrolled member's identifier (the child key).</param>
[EventType]
public record MemberEnrolled(MemberId MemberId);
