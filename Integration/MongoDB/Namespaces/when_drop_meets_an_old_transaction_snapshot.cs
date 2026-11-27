// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

using context = Cratis.Chronicle.MongoDB.Integration.Namespaces.when_drop_meets_an_old_transaction_snapshot.context;

namespace Cratis.Chronicle.MongoDB.Integration.Namespaces;

[Collection(MongoDBCollection.Name)]
public class when_drop_meets_an_old_transaction_snapshot(context context) : MongoDBGiven<context>(context)
{
    [Fact] void should_refuse_the_snapshot_writer_after_close() => Context.OldWriterWasRefused.ShouldBeTrue();
    [Fact] void should_install_the_replacement_identity() => Context.ReplacementIdentityIsInstalled.ShouldBeTrue();
    [Fact] void should_not_leak_old_events() => Context.EventsAfterRecreation.ShouldEqual(0L);
    [Fact] void should_not_leak_old_receipts() => Context.ReceiptsAfterRecreation.ShouldEqual(0L);

    public class context(ChronicleInProcessFixture fixture) : given.a_namespace_catalog_race(fixture)
    {
        readonly TaskCompletionSource _snapshot = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool OldWriterWasRefused;
        public bool ReplacementIdentityIsInstalled => _installedIdentity == _replacement.Value;
        public long EventsAfterRecreation => _eventsAfterRecreation;
        public long ReceiptsAfterRecreation => _receiptsAfterRecreation;

        async Task Because()
        {
            var writer = Start(WriteFromOldSnapshot);
            var removal = Task.CompletedTask;
            try
            {
                await WaitFor(_snapshot.Task, writer);
                (await _adminMarker.TryClose(_old)).ShouldBeTrue();
                removal = Start(DropAndRecreate);
                await WaitFor(_dropSubmitted.Task, removal);
            }
            finally
            {
                _release.TrySetResult();
            }
            await Task.WhenAll(writer, removal).WaitAsync(Timeout);
            await InspectReplacement();
        }

        static bool IsDefiniteAbort(MongoCommandException error)
        {
            if (error.Code is 112 or 251) return true;
            return error.HasErrorLabel("TransientTransactionError");
        }

        async Task WriteFromOldSnapshot()
        {
            using var session = await _writerClient.StartSessionAsync();
            Begin(session);
            var marker = _database.GetCollection<BsonDocument>(WellKnownCollectionNames.NamespaceMarker);
            (await marker.Find(session, FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync()).ShouldNotBeNull();
            _snapshot.SetResult();
            await _release.Task.WaitAsync(Timeout);
            try
            {
                if (!await _marker.TryFence(session, _old, CancellationToken.None))
                {
                    OldWriterWasRefused = true;
                    await session.AbortTransactionAsync();
                    return;
                }
                await WriteWitnesses(session);
                await session.CommitTransactionAsync();
            }
            catch (MongoCommandException error) when (IsDefiniteAbort(error))
            {
                OldWriterWasRefused = true;
            }
        }
    }
}
