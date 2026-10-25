// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Orleans.Storage.MongoDB.Serialization;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBSchemaServices;

[Collection(MongoDBCollection.Name)]
public class when_provisioning_with_runtime_serialization(MongoDBFixture fixture) : given.a_schema_database(fixture)
{
    readonly Dictionary<string, bool> _matchingIndexes = new(StringComparer.Ordinal);
    int _migrationExit;
    int _verificationExit;
    BsonDocument _stored;

    static string Signature(IEnumerable<BsonDocument> indexes) => string.Join('\n', indexes
        .OrderBy(index => index["name"].AsString, StringComparer.Ordinal)
        .Select(index => $"{index["name"]}:{index["key"].ToJson()}:{index.GetValue("unique", false)}:{index.GetValue("sparse", false)}:{index.GetValue("collation", BsonNull.Value)}"));

    async Task Establish()
    {
        await SaveConstraintDefinitions();
        await _namespace.CreateCollectionAsync("event-log+email+constraint");
        await _namespace.CreateCollectionAsync("event-log+retained-email+values+constraint");
    }

    async Task Because()
    {
        // Separate processes deliberately isolate SchemaCommandRunner's own BSON setup from SpecSerializationSetup.
        _migrationExit = await RunServer("--migrate-only");
        _verificationExit = await RunServer("--verify-schema");
        var runtime = _database.GetEventStoreDatabase(Store).GetNamespaceDatabase(Namespace);
        await runtime.GetEventSequenceCollectionFor(Concepts.EventSequences.EventSequenceId.Log).InsertOneAsync(
            new(
                1,
                CorrelationId.New(),
                [],
                [],
                "schema-spec",
                DateTimeOffset.UtcNow,
                EventSourceType.Default,
                "source",
                EventStreamType.All,
                EventStreamId.Default,
                [],
                new Dictionary<string, BsonDocument>(),
                new Dictionary<string, string>(),
                []) { PublicationId = "publication" });
        _stored = await _namespace.GetCollection<BsonDocument>(WellKnownCollectionNames.EventLog).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        var runtimePrefix = $"{_prefix}r_";
        var runtimeDatabase = new Database(_clients, _mongoOptions, Substitute.For<ICustomSerializers>(), Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = runtimePrefix }));
        await given.runtime_index_paths.Exercise(runtimeDatabase, _client.GetDatabase(DatabaseNames.ForEventStoreNamespace(Store, Namespace, runtimePrefix)));
        var runtimeNamespace = runtimeDatabase.GetEventStoreDatabase(Store).GetNamespaceDatabase(Namespace).GetCollection<BsonDocument>(WellKnownCollectionNames.EventSequences).Database;
        foreach (var collection in new[] { "event-log", "effect-receipts", "encryption-keys", "occurrences", "event-log+closed_streams", "event-log+email+constraint", "event-log+retained-email+values+constraint", WellKnownCollectionNames.AlertIncidents, WellKnownCollectionNames.FailedPartitions, WellKnownCollectionNames.InFlightEvents, WellKnownCollectionNames.ObserverDefinitionChangeProgress, WellKnownCollectionNames.ObserverDefinitionChangeContinuations, WellKnownCollectionNames.ObserverDefinitionChangeRestartPlans, WellKnownCollectionNames.ObserverDefinitionChangeInventory })
        {
            _matchingIndexes[collection] = Signature(await Indexes(_namespace, collection)) == Signature(await Indexes(runtimeNamespace, collection));
        }
    }

    [Fact] void should_migrate_with_the_server_command_setup() => _migrationExit.ShouldEqual(0);
    [Fact] void should_verify_with_the_server_command_setup() => _verificationExit.ShouldEqual(0);
    [Fact] void should_match_runtime_created_index_keys_and_options() => _matchingIndexes.Values.All(matches => matches).ShouldBeTrue();
    [Fact] void should_write_camel_case_runtime_fields() => _stored.Contains("eventSourceId").ShouldBeTrue();
    [Fact] void should_not_write_pascal_case_event_fields() => _stored.Contains("EventSourceId").ShouldBeFalse();
    [Fact] async Task should_index_the_runtime_publication_field() => (await Indexes(_namespace, "event-log")).Single(index => index["name"] == "publication_identity")["key"].AsBsonDocument.Contains("publicationId").ShouldBeTrue();
    [Fact] async Task should_index_the_composite_encryption_identity() => (await Indexes(_namespace, "encryption-keys")).Single(index => index["name"] == "id_identifier_id_revision")["key"].AsBsonDocument.Contains("_id.identifier").ShouldBeTrue();
    [Fact] async Task should_index_camel_case_stream_dimensions() => (await Indexes(_namespace, "event-log+closed_streams")).Single(index => index["name"] == "scope_owner")["key"].AsBsonDocument.Contains("streamType").ShouldBeTrue();

    async Task<int> RunServer(string action)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        var framework = directory.Name;
        var configuration = directory.Parent!.Name;
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Source/Kernel/Server/Server.csproj"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Cannot locate the server project for the isolated schema command spec.");
        var server = Path.Combine(directory.FullName, "Source/Kernel/Server/bin", configuration, framework, "Cratis.Chronicle.Server.dll");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = directory.FullName };
        foreach (var argument in new[] { server, action, "--event-store", Store, "--Cratis:Chronicle:Storage:Type=MongoDB", $"--Cratis:Chronicle:Storage:ConnectionDetails={ConnectionString}", $"--Cratis:Chronicle:Storage:DatabaseNamePrefix={_prefix}" }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the isolated schema command.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        await Task.WhenAll(output, error);
        var logs = Path.Combine(directory.FullName, ".ai-work/5284/e2e", _prefix);
        Directory.CreateDirectory(logs);
        await File.WriteAllTextAsync(Path.Combine(logs, $"{action.TrimStart('-')}.log"), $"{await output}\n{await error}");
        return process.ExitCode;
    }
}
