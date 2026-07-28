// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Execution;
using Samples;

namespace AspNetCore;

/// <summary>
/// Maps the web API surface exposing the SimpleConsole capabilities as HTTP endpoints.
/// </summary>
public static class Api
{
    /// <summary>
    /// Maps all the test app endpoints.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to map on.</param>
    /// <returns>The <see cref="WebApplication"/> for continuation.</returns>
    public static WebApplication MapSampleApi(this WebApplication app)
    {
        app.MapGet("/api/instance", (IEventStore eventStore) => new
        {
            Name = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? Environment.MachineName,
            ConnectionId = eventStore.Connection.Lifecycle.ConnectionId.Value,
            EventStore = eventStore.Name.Value
        });

        app.MapGet("/api/employees", async (IReadModels readModels) =>
        {
            var employees = new List<object>();
            foreach (var person in EmployeeData.Persons)
            {
                var state = await readModels.GetInstanceById<EmployeeState>(person.EventSourceId);
                employees.Add(new
                {
                    Id = person.EventSourceId,
                    person.FirstName,
                    person.LastName,
                    state.Title,
                    state.Email,
                    state.Address,
                    state.City,
                    state.Country
                });
            }

            return employees;
        });

        app.MapPost("/api/employees/{id}/promote", async (string id, IEventLog eventLog) =>
        {
            if (GetPerson(id) is not { } person)
            {
                return Results.NotFound();
            }

            var title = EmployeeData.Titles[Random.Shared.Next(EmployeeData.Titles.Length)];
            var result = await eventLog.Append(person.EventSourceId, new EmployeePromoted(title));
            return Results.Ok(new { Message = $"Promoted {person.FirstName} {person.LastName} to '{title}' at sequence {result.SequenceNumber.Value}" });
        });

        app.MapPost("/api/employees/{id}/move", async (string id, IEventLog eventLog) =>
        {
            if (GetPerson(id) is not { } person)
            {
                return Results.NotFound();
            }

            var address = EmployeeData.Addresses[Random.Shared.Next(EmployeeData.Addresses.Length)];
            var result = await eventLog.Append(person.EventSourceId, new EmployeeMoved(address.Street, address.City, address.ZipCode, address.Country));
            return Results.Ok(new { Message = $"Moved {person.FirstName} {person.LastName} to {address.Street}, {address.City} at sequence {result.SequenceNumber.Value}" });
        });

        app.MapPost("/api/employees/{id}/set-email", async (string id, IEventLog eventLog) =>
        {
            if (GetPerson(id) is not { } person)
            {
                return Results.NotFound();
            }

            var email = EmployeeData.GetEmailFor(person);
            var result = await eventLog.Append(person.EventSourceId, new EmployeeEmailSet(email));
            return Results.Ok(result.IsSuccess
                ? new { Success = true, Message = $"Set {person.FirstName} {person.LastName}'s email to {email} at sequence {result.SequenceNumber.Value}" }
                : new { Success = false, Message = $"Could not set email: {Violations(result)}" });
        });

        app.MapPost("/api/employees/{id}/steal-email", async (string id, IEventLog eventLog) =>
        {
            if (GetPerson(id) is not { } person)
            {
                return Results.NotFound();
            }

            var victim = NextPersonAfter(person);
            var email = EmployeeData.GetEmailFor(victim);
            var result = await eventLog.Append(person.EventSourceId, new EmployeeEmailSet(email));
            return Results.Ok(result.IsSuccess
                ? new { Success = true, Message = $"Unexpectedly took {email} at sequence {result.SequenceNumber.Value}" }
                : new { Success = false, Message = $"Rejected taking {victim.FirstName}'s email ({email}): {Violations(result)}" });
        });

        app.MapPost("/api/employees/{id}/transactional", async (string id, IEventStore eventStore) =>
        {
            if (GetPerson(id) is not { } person)
            {
                return Results.NotFound();
            }

            var alsoUpdate = NextPersonAfter(person);
            var title = EmployeeData.Titles[Random.Shared.Next(EmployeeData.Titles.Length)];
            var address = EmployeeData.Addresses[Random.Shared.Next(EmployeeData.Addresses.Length)];
            var secondTitle = EmployeeData.Titles[Random.Shared.Next(EmployeeData.Titles.Length)];

            var unitOfWork = eventStore.UnitOfWorkManager.Begin(CorrelationId.New());
            await eventStore.EventLog.Transactional.Append(person.EventSourceId, new EmployeePromoted(title));
            await eventStore.EventLog.Transactional.AppendMany(person.EventSourceId, [
                new EmployeeMoved(address.Street, address.City, address.ZipCode, address.Country)
            ]);
            await eventStore.EventLog.Transactional.Append(alsoUpdate.EventSourceId, new EmployeePromoted(secondTitle));
            await unitOfWork.Commit();

            return Results.Ok(new { Message = $"Committed staged events for {person.FirstName} {person.LastName} and {alsoUpdate.FirstName} {alsoUpdate.LastName}" });
        });

        app.MapPost("/api/customers/register", async (IEventLog eventLog) =>
        {
            var registered = new CustomerRegistered(SampleCustomer.Id, SampleCustomer.Email, SampleCustomer.FullName, SampleCustomer.PhoneNumber);
            var addressUpdated = new CustomerAddressUpdated(SampleCustomer.Id, SampleCustomer.StreetAddress, SampleCustomer.City, SampleCustomer.PostalCode, SampleCustomer.Country);
            var result = await eventLog.AppendMany(SampleCustomer.Id, [registered, addressUpdated]);

            return result.IsSuccess
                ? new { Success = true, Message = $"Registered {(string)SampleCustomer.FullName} ({SampleCustomer.Id}) with PII events up to sequence {result.SequenceNumbers.Last().Value}" }
                : new { Success = false, Message = $"Could not register {(string)SampleCustomer.FullName}: {string.Join("; ", result.ConstraintViolations.Select(violation => violation.Message))}" };
        });

        app.MapGet("/api/customers/sample", async (IReadModels readModels) =>
        {
            var customer = await readModels.GetInstanceById<Customer>(SampleCustomer.Id);
            if (string.IsNullOrEmpty(customer.Id))
            {
                return Results.NotFound(new { Message = $"No Customer read model found for {SampleCustomer.Id}. Register the customer first." });
            }

            return Results.Ok(new
            {
                customer.Id,
                FullName = (string)customer.FullName,
                Email = (string)customer.Email,
                PhoneNumber = (string)customer.PhoneNumber,
                StreetAddress = (string)customer.StreetAddress,
                City = (string)customer.City,
                PostalCode = (string)customer.PostalCode,
                customer.Country,
                customer.CustomerNumber,
                customer.AccountStatus,
                customer.TotalOrders
            });
        });

        app.MapGet("/api/reactor-invocations", (ReactorInvocationLog log) => log.All);

        return app;
    }

    static Person? GetPerson(string id) =>
        EmployeeData.Persons.FirstOrDefault(person => person.EventSourceId == id);

    static Person NextPersonAfter(Person person)
    {
        var index = Array.IndexOf(EmployeeData.Persons, person);
        return EmployeeData.Persons[(index + 1) % EmployeeData.Persons.Length];
    }

    static string Violations(AppendResult result) =>
        string.Join("; ", result.ConstraintViolations.Select(violation => violation.Message));
}
