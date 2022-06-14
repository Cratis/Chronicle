// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Events.Store.MongoDB;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.MongoDB;
using Aksio.Cratis.Json;
using Events.Accounts.Debit;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.TestData;

static class Program
{
    static readonly string[] _owners = new string[]
    {
        "aae77be6-21ce-4dc6-bf33-e97014c2ba3b",
        "6a48dc85-086b-4ab3-ba4f-44259eb574ca",
        "acd5cbb4-09c4-4a69-bafb-7e784d7de8bd",
        "a9ade696-9958-4f24-9a9d-099405c93b28",
        "b676372f-297b-4f3b-8f4e-94555512ba31"
    };

    static readonly Dictionary<string, List<string>> _accountsPerPerson = new();
    static JsonSerializerOptions? _serializerOptions;

    static IMongoCollection<Event>? _eventLog;
    static IMongoCollection<EventSequenceState>? _eventSequenceState;

    public static async Task Main()
    {
        _serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
                {
                    new ConceptAsJsonConverterFactory()
                }
        };

        var types = new Types.Types();
        var defaults = new MongoDBDefaults(types);
        defaults.Initialize();

        var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");
        var client = new MongoClient(settings);
        var database = client.GetDatabase("bank-dev-event-store");
        _eventLog = database.GetCollection<Event>("event-log");
        _eventSequenceState = database.GetCollection<EventSequenceState>("event-sequences");

        var rnd = new Random();

        var sequenceNumber = (await GetSequence()).SequenceNumber;

        Console.WriteLine($"Start generating test data at sequence number: {sequenceNumber}");
        var occurred = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(30));

        foreach (var owner in _owners)
        {
            var accounts = new List<string>();

            for (var i = 0; i < rnd.Next() % 10; i++)
            {
                var account = Guid.NewGuid().ToString();
                accounts.Add(account);
                await Append(
                    sequenceNumber,
                    account,
                    occurred,
                    new DebitAccountOpened($"account-{i}", owner));

                sequenceNumber++;

                occurred = occurred.Add(TimeSpan.FromSeconds(rnd.NextDouble() * 100));

                Console.WriteLine($"Added account {account} for {owner}");
            }

            _accountsPerPerson[owner] = accounts;
        }

        Console.WriteLine("Add operations on accounts for owners");
        for (var i = 0; i < 200; i++)
        {
            var owner = _owners[rnd.Next() % _owners.Length];
            if (!_accountsPerPerson.ContainsKey(owner) || _accountsPerPerson[owner].Count == 0)
            {
                continue;
            }
            var account = _accountsPerPerson[owner][rnd.Next() % _accountsPerPerson[owner].Count];
            var deposit = rnd.Next() % 2;

            object @event;

            if (deposit == 0)
            {
                @event = new DepositToDebitAccountPerformed(rnd.NextDouble() * 500);
            }
            else
            {
                @event = new WithdrawalFromDebitAccountPerformed(rnd.NextDouble() * 500);
            }

            await Append(
                sequenceNumber,
                account,
                occurred,
                @event);

            sequenceNumber++;
            await WriteSequence(sequenceNumber);

            Console.Write(".");

            occurred = occurred.Add(TimeSpan.FromSeconds(rnd.NextDouble() * 100));
        }
    }

    static async Task Append(EventSequenceNumber sequenceNumber, EventSourceId eventSourceId, DateTimeOffset occurred, object content)
    {
        var eventTypeAttribute = content.GetType().GetCustomAttribute<EventTypeAttribute>();
        var eventType = eventTypeAttribute!.Type;

        var contentAsJson = JsonSerializer.Serialize(content, _serializerOptions!);
        var contentAsBson = BsonDocument.Parse(contentAsJson);
        contentAsBson.Remove("_t");

        var @event = new Event(
            sequenceNumber,
            CorrelationId.New(),
            CausationId.System,
            CausedBy.System,
            eventType.Id,
            occurred,
            DateTimeOffset.MinValue,
            eventSourceId,
            new Dictionary<string, BsonDocument>
            {
                { "1", contentAsBson }
            },
            Array.Empty<EventCompensation>());

        await _eventLog!.InsertOneAsync(@event);
    }

    static async Task<EventSequenceState> GetSequence()
    {
        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, Guid>("_id"), Guid.Empty);
        var cursor = await _eventSequenceState.FindAsync(filter);
        return await cursor.FirstOrDefaultAsync() ?? new EventSequenceState();
    }

    static Task WriteSequence(EventSequenceNumber sequenceNumber)
    {
        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, Guid>("_id"), Guid.Empty);
        return _eventSequenceState!.UpdateOneAsync(
            filter,
            Builders<EventSequenceState>.Update.Set(_ => _.SequenceNumber, sequenceNumber),
            new() { IsUpsert = true });
    }
}
