// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Telemetry MUST be built first so the TracerProvider is active before
// the Chronicle client creates its ActivitySource spans.
using Cratis.Chronicle;
using Cratis.Chronicle.Sinks;
using Cratis.Execution;
using Microsoft.Extensions.Logging;
using TestApp;

using var telemetry = Telemetry.Build();

using var loggerFactory = LoggerFactory
    .Create(static builder => builder
        .AddConsole());

// Parse database argument (default: mongodb)
var database = args.Length > 0 ? args[0].ToLowerInvariant() : "mongodb";
var sinkType = database switch
{
    "postgresql" => WellKnownSinkTypes.SQL,
    "mssql" => WellKnownSinkTypes.SQL,
    "sqlite" => WellKnownSinkTypes.SQL,
    "mongodb" or _ => WellKnownSinkTypes.MongoDB,
};

var options = ChronicleOptions.FromConnectionString("chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35000");
options.DefaultSinkTypeId = sinkType;

Console.WriteLine($"Connecting to Chronicle... (Database: {database}, Sink: {sinkType})");
using var client = new ChronicleClient(options, loggerFactory: loggerFactory);
var store = await client.GetEventStore("TestStoreCS");

var random = new Random();
var selectedIndex = 0;
var userIndex = 0;

WriteInstructions();
WriteSelectedEmployee(selectedIndex, userIndex);

while (true)
{
    var keyInfo = Console.ReadKey(true);

    switch (keyInfo.Key)
    {
        case ConsoleKey.D1:
        case ConsoleKey.NumPad1:
            selectedIndex = 0;
            WriteSelectedEmployee(selectedIndex, userIndex);
            break;
        case ConsoleKey.D2:
        case ConsoleKey.NumPad2:
            selectedIndex = 1;
            WriteSelectedEmployee(selectedIndex, userIndex);
            break;
        case ConsoleKey.D3:
        case ConsoleKey.NumPad3:
            selectedIndex = 2;
            WriteSelectedEmployee(selectedIndex, userIndex);
            break;
        case ConsoleKey.I:
            userIndex = (userIndex + 1) % 3;
            WriteSelectedUser(userIndex);
            break;
        case ConsoleKey.P:
            await Promote(selectedIndex, random);
            break;
        case ConsoleKey.A:
            await Move(selectedIndex, random);
            break;
        case ConsoleKey.E:
            await SetEmail(selectedIndex);
            break;
        case ConsoleKey.U:
            await StealEmail(selectedIndex);
            break;
        case ConsoleKey.R:
            await DisplayReadModel(selectedIndex);
            break;
        case ConsoleKey.T:
            await TransactionalUpdate(selectedIndex, random);
            break;
        case ConsoleKey.C:
            await ComplianceDemo.RegisterCustomerWithPii(store);
            break;
        case ConsoleKey.V:
            await ComplianceDemo.ShowCustomerReadModel(store);
            break;
        case ConsoleKey.H:
            WriteInstructions();
            break;
        case ConsoleKey.Q:
        case ConsoleKey.Escape:
            Console.WriteLine("Exiting...");
            return;
    }
}

async Task Promote(int index, Random random)
{
    var person = EmployeeData.Persons[index];
    var title = EmployeeData.Titles[random.Next(EmployeeData.Titles.Length)];
    var @event = new EmployeePromoted(title);
    var result = await store.EventLog.Append(person.EventSourceId, @event);
    Console.WriteLine($"[{person.EventSourceId}] Promoted {person.FirstName} {person.LastName} to '{title}' at sequence {result.SequenceNumber}");
}

async Task Move(int index, Random random)
{
    var person = EmployeeData.Persons[index];
    var address = EmployeeData.Addresses[random.Next(EmployeeData.Addresses.Length)];
    var @event = new EmployeeMoved(address.Street, address.City, address.ZipCode, address.Country);
    var result = await store.EventLog.Append(person.EventSourceId, @event);
    Console.WriteLine($"[{person.EventSourceId}] Moved {person.FirstName} {person.LastName} to {address.Street}, {address.City} at sequence {result.SequenceNumber}");
}

async Task SetEmail(int index)
{
    var person = EmployeeData.Persons[index];
    var email = EmployeeData.GetEmailFor(person);
    var @event = new EmployeeEmailSet(email);
    var result = await store.EventLog.Append(person.EventSourceId, @event);

    if (result.IsSuccess)
    {
        Console.WriteLine($"[{person.EventSourceId}] Set {person.FirstName} {person.LastName}'s email to {email} at sequence {result.SequenceNumber}");
    }
    else
    {
        Console.WriteLine($"[{person.EventSourceId}] Could not set email: {string.Join("; ", result.ConstraintViolations.Select(v => v.Message))}");
    }
}

async Task StealEmail(int index)
{
    var person = EmployeeData.Persons[index];
    var victim = EmployeeData.Persons[(index + 1) % EmployeeData.Persons.Length];
    var email = EmployeeData.GetEmailFor(victim);
    var @event = new EmployeeEmailSet(email);
    var result = await store.EventLog.Append(person.EventSourceId, @event);

    if (result.IsSuccess)
    {
        Console.WriteLine($"[{person.EventSourceId}] Unexpectedly took {email} at sequence {result.SequenceNumber}");
    }
    else
    {
        Console.WriteLine($"[{person.EventSourceId}] Rejected taking {victim.FirstName}'s email ({email}): {string.Join("; ", result.ConstraintViolations.Select(v => v.Message))}");
    }
}

async Task DisplayReadModel(int index)
{
    var person = EmployeeData.Persons[index];
    var state = await store.ReadModels.GetInstanceById<EmployeeState>(person.EventSourceId);
    Console.WriteLine($"[read-model] {person.FirstName} {person.LastName}: {state.Title} <{(string.IsNullOrEmpty(state.Email) ? "no email yet" : state.Email)}> @ {(string.IsNullOrEmpty(state.Address) ? "no address yet" : state.Address)}");
}

async Task TransactionalUpdate(int index, Random random)
{
    var selected = EmployeeData.Persons[index];
    var alsoUpdate = EmployeeData.Persons[(index + 1) % EmployeeData.Persons.Length];

    var selectedTitle = EmployeeData.Titles[random.Next(EmployeeData.Titles.Length)];
    var selectedAddress = EmployeeData.Addresses[random.Next(EmployeeData.Addresses.Length)];
    var secondTitle = EmployeeData.Titles[random.Next(EmployeeData.Titles.Length)];

    var unitOfWork = store.UnitOfWorkManager.Begin(CorrelationId.New());
    await store.EventLog.Transactional.Append(selected.EventSourceId, new EmployeePromoted(selectedTitle));
    await store.EventLog.Transactional.AppendMany(selected.EventSourceId, [
        new EmployeeMoved(selectedAddress.Street, selectedAddress.City, selectedAddress.ZipCode, selectedAddress.Country)
    ]);
    await store.EventLog.Transactional.Append(alsoUpdate.EventSourceId, new EmployeePromoted(secondTitle));
    await unitOfWork.Commit();

    Console.WriteLine($"[transaction] Committed staged events for {selected.FirstName} {selected.LastName} and {alsoUpdate.FirstName} {alsoUpdate.LastName}");
}

void WriteInstructions()
{
#pragma warning disable MA0136
    Console.WriteLine(
        """

        Use 1-3 to select an employee. Then:
          P = Promote          A = Move (change address)
          E = Set email        U = Try to take the next employee's email (constraint violation)
          R = Read model       T = Transactional update
          C = Register customer with PII   V = View customer PII read model
          I = Switch user (cycle: Alice Smith → Bob Jones → System)
          H or ? = Show this menu          Q = Quit

        """);
#pragma warning restore MA0136
}

void WriteSelectedEmployee(int employeeIndex, int userIndex)
{
    var person = EmployeeData.Persons[employeeIndex];
    var user = GetUserName(userIndex);
    Console.WriteLine($"Selected  [{employeeIndex + 1}] {person.FirstName} {person.LastName} ({person.EventSourceId})");
    Console.WriteLine($"Acting as [{userIndex + 1}] {user}");
}

void WriteSelectedUser(int userIndex)
{
    var user = GetUserName(userIndex);
    Console.WriteLine($"\nSwitched to user [{userIndex + 1}] {user}");
}

string GetUserName(int index) => index switch
{
    0 => "Alice Smith (alice.smith)",
    1 => "Bob Jones (bob.jones)",
    _ => "System"
};
