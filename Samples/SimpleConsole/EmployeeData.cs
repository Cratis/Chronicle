// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples;

public record Person(string EventSourceId, string FirstName, string LastName);

public record Address(string Street, string City, string ZipCode, string Country);

public static class EmployeeData
{
    public static readonly Person[] Persons = GeneratePersons(50);

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

    public static string GetEmailFor(Person person) =>
        $"{person.FirstName.ToLowerInvariant()}.{person.LastName.ToLowerInvariant()}@example.com";

    /// <summary>
    /// Builds the employee roster the sample can generate data for. The first three keep the
    /// original names the sample has always used; the rest are built deterministically from name
    /// pools so the roster - and the event source ids it maps to - is identical on every run.
    /// </summary>
    /// <param name="count">The number of employees to generate.</param>
    /// <returns>The generated <see cref="Person"/> array.</returns>
    static Person[] GeneratePersons(int count)
    {
        string[] firstNames = ["Naledi", "Ravi", "Elena", "Liam", "Fatima", "Wei", "Ingrid", "Kofi", "Priya", "Diego"];
        string[] lastNames = ["Mokoena", "Patel", "Vasquez", "Larsen", "Haddad"];

        var persons = new Person[count];
        persons[0] = new("employee-1", "Ada", "Lovelace");
        persons[1] = new("employee-2", "Grace", "Hopper");
        persons[2] = new("employee-3", "Alan", "Turing");

        for (var i = 3; i < count; i++)
        {
            var lastNameGroup = i / firstNames.Length;
            var firstName = firstNames[i % firstNames.Length];
            var lastName = lastNames[lastNameGroup % lastNames.Length];
            persons[i] = new($"employee-{i + 1}", firstName, lastName);
        }

        return persons;
    }
}
