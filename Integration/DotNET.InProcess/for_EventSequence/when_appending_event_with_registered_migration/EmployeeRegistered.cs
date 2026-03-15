// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_event_with_registered_migration;

/// <summary>
/// Generation 2 of EmployeeRegistered — the FullName property has been split into separate FirstName and LastName fields.
/// </summary>
/// <param name="FirstName">The employee's first name.</param>
/// <param name="LastName">The employee's last name.</param>
[EventType("EmployeeRegistered", generation: 2)]
public record EmployeeRegistered(string FirstName, string LastName);
