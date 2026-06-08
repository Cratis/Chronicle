// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace TestApp;

[EventType]
public record EmployeeHired(string FirstName, string LastName, string Title);

[EventType]
public record EmployeePromoted(string NewTitle);

[EventType]
public record EmployeeAddressSet(string Address, string City, string ZipCode, string Country);

[EventType]
public record EmployeeMoved(string Address, string City, string ZipCode, string Country);

[EventType]
public record EmployeeEmailSet(string Email);

[FromEvent<EmployeeHired>]
[FromEvent<EmployeePromoted>]
[FromEvent<EmployeeAddressSet>]
[FromEvent<EmployeeMoved>]
[FromEvent<EmployeeEmailSet>]
public record EmployeeState(
    string FirstName = "",
    string LastName = "",
    string Title = "",
    string Email = "",
    string Address = "",
    string City = "",
    string ZipCode = "",
    string Country = "");
