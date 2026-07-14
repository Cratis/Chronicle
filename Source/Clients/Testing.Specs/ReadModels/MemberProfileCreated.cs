// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event creating a member's profile; lives on the member's own event source and is joined into a
/// roster's member child rows.
/// </summary>
/// <param name="MemberName">The member's name.</param>
[EventType]
public record MemberProfileCreated(string MemberName);
