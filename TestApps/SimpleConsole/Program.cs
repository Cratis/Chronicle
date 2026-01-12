// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
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

Console.WriteLine("Appending event...");
var @event = new MyEvent();
var result = await store.EventLog.Append("MyThing", @event);
Console.WriteLine($"Event appended. {result.SequenceNumber}");
