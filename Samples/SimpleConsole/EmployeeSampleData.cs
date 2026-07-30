// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Samples;

/// <summary>
/// Represents the outcome of generating the employee sample data.
/// </summary>
/// <param name="Created">The number of employees whose events were appended.</param>
/// <param name="Skipped">The number of employees that were rejected, typically because they already exist.</param>
/// <param name="Reasons">The distinct reasons employees were rejected.</param>
public record EmployeeSampleDataResult(int Created, int Skipped, IEnumerable<string> Reasons);

/// <summary>
/// Generates the sample employee data set on demand - the full roster from
/// <see cref="EmployeeData.Persons"/>, each with a career history of promotions and moves.
/// </summary>
/// <remarks>
/// This is deliberately driven from a keystroke rather than produced when the sample starts, so the
/// store only gains data when it is asked for.
/// </remarks>
public static class EmployeeSampleData
{
    /// <summary>
    /// Appends the hire, email, address and career-history events for every employee.
    /// </summary>
    /// <param name="store">The <see cref="IEventStore"/> to append to.</param>
    /// <returns>An <see cref="EmployeeSampleDataResult"/> describing what was appended.</returns>
    /// <remarks>
    /// Each employee's events are appended as a single transaction, so re-running against a store
    /// that already holds the data leaves it untouched - the <c>UniqueEmployeeHire</c> constraint
    /// rejects the whole batch for an employee that has already been hired.
    /// </remarks>
    public static async Task<EmployeeSampleDataResult> Generate(IEventStore store)
    {
        var created = 0;
        var skipped = 0;
        var reasons = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < EmployeeData.Persons.Length; index++)
        {
            var person = EmployeeData.Persons[index];
            var result = await store.EventLog.AppendMany(person.EventSourceId, EventsFor(person, index));

            if (result.IsSuccess)
            {
                created++;
                continue;
            }

            skipped++;
            foreach (var violation in result.ConstraintViolations)
            {
                reasons.Add(violation.Message.Value);
            }

            foreach (var error in result.Errors)
            {
                reasons.Add(error.Value);
            }
        }

        return new EmployeeSampleDataResult(created, skipped, reasons);
    }

    /// <summary>
    /// Builds the full set of events for a single employee - the hire, their email and address,
    /// followed by the career history layered on top.
    /// </summary>
    /// <param name="person">The <see cref="Person"/> to build events for.</param>
    /// <param name="index">The employee's index in <see cref="EmployeeData.Persons"/>.</param>
    /// <returns>The events to append for the employee.</returns>
    static IEnumerable<object> EventsFor(Person person, int index)
    {
        var addressIndex = index % EmployeeData.Addresses.Length;
        var titleIndex = index % EmployeeData.Titles.Length;
        var address = EmployeeData.Addresses[addressIndex];

        yield return new EmployeeHired(person.FirstName, person.LastName, EmployeeData.Titles[titleIndex]);
        yield return new EmployeeEmailSet(EmployeeData.GetEmailFor(person));
        yield return new EmployeeAddressSet(address.Street, address.City, address.ZipCode, address.Country);

        foreach (var @event in GenerateActivity(index, titleIndex, addressIndex))
        {
            yield return @event;
        }
    }

    /// <summary>
    /// Generates a career history of promotions and moves for an employee, layered on top of their
    /// hire. Uses a <see cref="Random"/> seeded by the employee's index so the generated set is
    /// identical on every run.
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
