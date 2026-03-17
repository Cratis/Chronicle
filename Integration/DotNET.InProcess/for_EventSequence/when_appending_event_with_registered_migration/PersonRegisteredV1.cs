// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_event_with_registered_migration;

/// <summary>
/// Generation 1 of <see cref="PersonRegistered"/> — carries the full name as a single string.
/// </summary>
/// <param name="FullName">The person's combined first and last name.</param>
[EventType("PersonRegistered", generation: 1)]
public record PersonRegisteredV1(string FullName);
