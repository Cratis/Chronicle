// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.MongoDB.Integration.Namespaces.when_a_cached_session_targets_a_recreated_database.context;

namespace Cratis.Chronicle.MongoDB.Integration.Namespaces;

[Collection(MongoDBCollection.Name)]
public class when_a_cached_session_targets_a_recreated_database(context context) : MongoDBGiven<context>(context)
{
    [Fact] void should_not_admit_the_retired_identity() => Context.FenceMatched.ShouldBeFalse();
    [Fact] void should_retain_the_replacement_identity() => Context.ReplacementIdentityIsInstalled.ShouldBeTrue();
    [Fact] void should_not_write_old_events() => Context.EventsAfterRecreation.ShouldEqual(0L);
    [Fact] void should_not_write_old_receipts() => Context.ReceiptsAfterRecreation.ShouldEqual(0L);

    public class context(ChronicleInProcessFixture fixture) : given.a_namespace_catalog_race(fixture)
    {
        public bool FenceMatched;
        public bool ReplacementIdentityIsInstalled => _installedIdentity == _replacement.Value;
        public long EventsAfterRecreation => _eventsAfterRecreation;
        public long ReceiptsAfterRecreation => _receiptsAfterRecreation;

        async Task Because()
        {
            using var session = await _writerClient.StartSessionAsync();
            Begin(session);
            await CloseDropAndRecreate();
            FenceMatched = await _marker.TryFence(session, _old, CancellationToken.None);
            if (FenceMatched) await WriteWitnesses(session);
            await session.CommitTransactionAsync();
            await InspectReplacement();
        }
    }
}
