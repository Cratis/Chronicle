// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Seeding;

namespace Samples;

public class EmployeeSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        for (var i = 0; i < EmployeeData.Persons.Length; i++)
        {
            var person = EmployeeData.Persons[i];
            var addressIndex = i % EmployeeData.Addresses.Length;
            var titleIndex = i % EmployeeData.Titles.Length;
            var email = EmployeeData.GetEmailFor(person);

            var events = new List<object>
            {
                new EmployeeHired(person.FirstName, person.LastName, EmployeeData.Titles[titleIndex]),
                new EmployeeEmailSet(email),
                new EmployeeAddressSet(
                    EmployeeData.Addresses[addressIndex].Street,
                    EmployeeData.Addresses[addressIndex].City,
                    EmployeeData.Addresses[addressIndex].ZipCode,
                    EmployeeData.Addresses[addressIndex].Country)
            };

            events.AddRange(GenerateActivity(i, titleIndex, addressIndex));

            builder.ForEventSource(person.EventSourceId, events);
        }
    }

    /// <summary>
    /// Generates a career history of promotions and moves for an employee, layered on top of their
    /// hire. Uses a <see cref="Random"/> seeded by the employee's index so re-running seeding against
    /// an already-populated store stays idempotent - Chronicle only skips a seed entry when its
    /// content matches exactly.
    /// </summary>
    /// <param name="employeeIndex">The employee's index in <see cref="EmployeeData.Persons"/>, used as the random seed.</param>
    /// <param name="titleIndex">The employee's initial index into <see cref="EmployeeData.Titles"/>.</param>
    /// <param name="addressIndex">The employee's initial index into <see cref="EmployeeData.Addresses"/>.</param>
    /// <returns>The generated <see cref="EmployeePromoted"/> and <see cref="EmployeeMoved"/> events.</returns>
    static IEnumerable<object> GenerateActivity(int employeeIndex, int titleIndex, int addressIndex)
    {
        var random = new Random(employeeIndex);

        var promotions = random.Next(0, 4);
        for (var p = 0; p < promotions; p++)
        {
            titleIndex = (titleIndex + 1) % EmployeeData.Titles.Length;
            yield return new EmployeePromoted(EmployeeData.Titles[titleIndex]);
        }

        var moves = random.Next(0, 3);
        for (var m = 0; m < moves; m++)
        {
            addressIndex = (addressIndex + 1) % EmployeeData.Addresses.Length;
            var address = EmployeeData.Addresses[addressIndex];
            yield return new EmployeeMoved(address.Street, address.City, address.ZipCode, address.Country);
        }
    }
}
