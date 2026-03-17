// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_events_with_multiple_generation_migrations;

/// <summary>
/// Generation 1 of UserRegistered — carries the full name as a single string.
/// </summary>
/// <param name="FullName">The user's combined first and last name.</param>
[EventType("UserRegistered", generation: 1)]
public record UserRegisteredV1(string FullName);
