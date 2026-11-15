// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Imports.for_Import.given;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Imports;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ImportsStorage = Cratis.Chronicle.Storage.InMemory.Imports.ImportStorage;

namespace Cratis.Chronicle.Imports.for_ImportRetention;

public class when_sweeping : Specification
{
    const string EventStore = "store";
    const string Namespace = "namespace";

    readonly SettableTime _time = new() { Now = DateTimeOffset.UtcNow };
    readonly ImportsStorage _imports = new();
    readonly Dictionary<ImportId, IImport> _grains = [];
    ImportRetention _retention;
    ImportId _openAndIdle;
    ImportId _committingAndStalled;
    ImportId _committingAndBusy;
    ImportId _failedLongAgo;
    ImportId _failedRecentlyWithAnOrphanChunk;

    async Task Establish()
    {
        var old = _time.Now - TimeSpan.FromHours(25);
        _openAndIdle = await Import(ImportState.Staging, old);
        _committingAndStalled = await Import(ImportState.Committing, old);
        _committingAndBusy = await Import(ImportState.Committing, _time.Now);
        _failedLongAgo = await Import(ImportState.Failed, old);
        _failedRecentlyWithAnOrphanChunk = await Import(ImportState.Failed, _time.Now);
        await _imports.AddChunk(new StagedImportChunk(_failedRecentlyWithAnOrphanChunk, 0, 0, 0, [], []));

        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        namespaceStorage.Imports.Returns(_imports);
        var namespaces = Substitute.For<INamespaceStorage>();
        namespaces.GetAll().Returns([new NamespaceState(Namespace, DateTimeOffset.UnixEpoch)]);
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreStorage.Namespaces.Returns(namespaces);
        eventStoreStorage.GetNamespace(Namespace).Returns(namespaceStorage);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStores().Returns([(EventStoreName)EventStore]);
        storage.GetEventStore(EventStore).Returns(eventStoreStorage);

        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IImport>(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => _grains[ImportKey.Parse(call.ArgAt<string>(0)).ImportId]);
        _retention = new(storage, grainFactory, Options.Create(new ChronicleOptions()), NullLogger<ImportRetention>.Instance, _time);
    }

    async Task Because() => await _retention.Sweep();

    void Destroy() => _retention.Dispose();

    [Fact] async Task should_abandon_an_open_import_that_outlived_the_retention() => await _grains[_openAndIdle].Received(1).AbandonIfExpired();
    [Fact] async Task should_fail_a_commit_that_stopped_making_progress() => await _grains[_committingAndStalled].Received(1).FailIfStalled();
    [Fact] async Task should_leave_a_commit_that_is_making_progress() => await _grains[_committingAndBusy].DidNotReceive().FailIfStalled();
    [Fact] async Task should_remove_an_import_that_ended_longer_ago_than_the_retention() => await _grains[_failedLongAgo].Received(1).RemoveIfExpired();
    [Fact] async Task should_keep_an_import_that_ended_recently() => await _grains[_failedRecentlyWithAnOrphanChunk].DidNotReceive().RemoveIfExpired();
    [Fact] async Task should_discard_staging_left_behind_by_an_ended_import() => (await _imports.GetChunkSummaries(_failedRecentlyWithAnOrphanChunk)).ShouldBeEmpty();

    async Task<ImportId> Import(ImportState state, DateTimeOffset lastActivity)
    {
        var id = ImportId.New();
        await _imports.Save(new ImportRecord(id, EventSequenceId.Log, OnInvalidEvent.Reject, lastActivity) { State = state, LastActivity = lastActivity });
        _grains[id] = Substitute.For<IImport>();
        return id;
    }
}
