// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestApp;

public record Person(string EventSourceId, string FirstName, string LastName);

public record Address(string Street, string City, string ZipCode, string Country);

public static class EmployeeData
{
    public static readonly Person[] Persons =
    [
        new("employee-1", "Ada", "Lovelace"),
        new("employee-2", "Grace", "Hopper"),
        new("employee-3", "Alan", "Turing")
    ];

    public static readonly Address[] Addresses =
    [
        new("221B Baker Street", "London", "NW1 6XE", "UK"),
        new("1600 Amphitheatre Parkway", "Mountain View", "94043", "USA"),
        new("1 Infinite Loop", "Cupertino", "95014", "USA"),
        new("5 Wall Street", "New York", "10005", "USA")
    ];

    public static readonly string[] Titles =
    [
        "Software Engineer",
        "Senior Engineer",
        "Principal Engineer",
        "Engineering Manager",
        "Architect"
    ];
}
