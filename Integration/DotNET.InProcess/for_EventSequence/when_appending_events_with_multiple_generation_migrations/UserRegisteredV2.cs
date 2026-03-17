// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_events_with_multiple_generation_migrations;

/// <summary>
/// Generation 2 of UserRegistered — the FullName property has been split into FirstName and LastName.
/// </summary>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
[EventType("UserRegistered", generation: 2)]
public record UserRegisteredV2(string FirstName, string LastName);
