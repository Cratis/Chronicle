// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event opening a membership roster for a group; lives on the group's own event source.
/// </summary>
/// <param name="GroupName">The group name.</param>
[EventType]
public record RosterOpened(string GroupName);
