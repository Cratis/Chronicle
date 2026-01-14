// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Seeding;

namespace TestApp;

public class EmployeeSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        for (var i = 0; i < EmployeeData.Persons.Length; i++)
        {
            var person = EmployeeData.Persons[i];
            var address = EmployeeData.Addresses[i % EmployeeData.Addresses.Length];
            var title = EmployeeData.Titles[i % EmployeeData.Titles.Length];

            builder.ForEventSource(
                person.EventSourceId,
                [
                    new PersonHired(person.FirstName, person.LastName, title),
                    new EmployeeAddressSet(address.Street, address.City, address.ZipCode, address.Country)
                ]);
        }
    }
}
