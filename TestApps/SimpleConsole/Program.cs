// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Execution;
using Microsoft.Extensions.Logging;
using TestApp;

using var loggerFactory = LoggerFactory
    .Create(static builder => builder
        .AddConsole());

var options = ChronicleOptions.FromConnectionString("chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35000");
options.LoggerFactory = loggerFactory;

Console.WriteLine("Connecting to Chronicle...");
using var client = new ChronicleClient(options);
var store = await client.GetEventStore("TestStore");

Console.WriteLine("Registering artifacts and seed data...");
await store.DiscoverAll();
await store.RegisterAll();

var random = new Random();
var selectedIndex = 0;

WriteInstructions();
WriteSelectedPerson(selectedIndex);

while (true)
{
    var keyInfo = Console.ReadKey(true);

    switch (keyInfo.Key)
    {
        case ConsoleKey.D1:
        case ConsoleKey.NumPad1:
            selectedIndex = 0;
            WriteSelectedPerson(selectedIndex);
            break;
        case ConsoleKey.D2:
        case ConsoleKey.NumPad2:
            selectedIndex = 1;
            WriteSelectedPerson(selectedIndex);
            break;
        case ConsoleKey.D3:
        case ConsoleKey.NumPad3:
            selectedIndex = 2;
            WriteSelectedPerson(selectedIndex);
            break;
        case ConsoleKey.P:
            await Promote(selectedIndex, random, CorrelationId.New());
            break;
        case ConsoleKey.A:
            await Move(selectedIndex, random, CorrelationId.New());
            break;
        case ConsoleKey.Q:
        case ConsoleKey.Escape:
            Console.WriteLine("Exiting...");
            return;
    }
}

// Promotes the selected employee to a random title and appends the event.
async Task Promote(int index, Random random, CorrelationId correlationId)
{
    var person = EmployeeData.Persons[index];
    var title = EmployeeData.Titles[random.Next(EmployeeData.Titles.Length)];
    var @event = new EmployeePromoted(title);
    var result = await store.EventLog.Append(person.EventSourceId, @event, correlationId: correlationId);
    Console.WriteLine($"[{person.EventSourceId}] Promoted {person.FirstName} {person.LastName} to '{title}' at sequence {result.SequenceNumber}");
}

// Moves the selected employee to a random address and appends the event.
async Task Move(int index, Random random, CorrelationId correlationId)
{
    var person = EmployeeData.Persons[index];
    var address = EmployeeData.Addresses[random.Next(EmployeeData.Addresses.Length)];
    var @event = new EmployeeMoved(address.Street, address.City, address.ZipCode, address.Country);
    var result = await store.EventLog.Append(person.EventSourceId, @event, correlationId: correlationId);
    Console.WriteLine($"[{person.EventSourceId}] Moved {person.FirstName} {person.LastName} to {address.Street}, {address.City} at sequence {result.SequenceNumber}");
}

// Writes the currently selected employee to the console.
void WriteSelectedPerson(int index)
{
    var person = EmployeeData.Persons[index];
    Console.WriteLine($"Selected [{index + 1}] {person.FirstName} {person.LastName} ({person.EventSourceId})");
}

// Writes the available keyboard controls to the console.
void WriteInstructions()
{
    Console.WriteLine("Use 1-3 to select employee. P=Promote, A=Move, Q=Quit.");
}
