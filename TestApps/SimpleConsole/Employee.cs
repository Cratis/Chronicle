// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace TestApp;

[EventType]
public record PersonHired(string FirstName, string LastName, string Title);

[EventType]
public record EmployeePromoted(string Title);

[EventType]
public record EmployeeAddressSet(string Address, string City, string ZipCode, string Country);

[EventType]
public record EmployeeMoved(string Address, string City, string ZipCode, string Country);

[FromEvent<PersonHired>]
[FromEvent<EmployeePromoted>]
[FromEvent<EmployeeAddressSet>]
[FromEvent<EmployeeMoved>]
public record Employee(
    string FirstName,
    string LastName,
    string Address,
    string Title,
    string City,
    string ZipCode,
    string Country);
