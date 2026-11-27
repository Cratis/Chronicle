// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.MongoDB.Integration.Namespaces.when_close_and_drop_meet_a_fenced_transaction.context;

namespace Cratis.Chronicle.MongoDB.Integration.Namespaces;

[Collection(MongoDBCollection.Name)]
public class when_close_and_drop_meet_a_fenced_transaction(context context) : MongoDBGiven<context>(context)
{
    [Fact] void should_not_submit_drop_while_the_old_fence_is_held() => Context.DropWasBlocked.ShouldBeTrue();
    [Fact] void should_commit_the_old_transaction_before_removal_completes() => Context.OldCommitAcknowledged.ShouldBeTrue();
    [Fact] void should_install_only_the_replacement_identity() => Context.ReplacementIdentityIsInstalled.ShouldBeTrue();
    [Fact] void should_not_move_old_events_into_the_recreated_database() => Context.EventsAfterRecreation.ShouldEqual(0L);
    [Fact] void should_not_move_old_receipts_into_the_recreated_database() => Context.ReceiptsAfterRecreation.ShouldEqual(0L);

    public class context(ChronicleInProcessFixture fixture) : given.a_namespace_catalog_race(fixture)
    {
        readonly TaskCompletionSource _fenced = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool DropWasBlocked;
        public bool OldCommitAcknowledged;
        public bool ReplacementIdentityIsInstalled => _installedIdentity == _replacement.Value;
        public long EventsAfterRecreation => _eventsAfterRecreation;
        public long ReceiptsAfterRecreation => _receiptsAfterRecreation;

        async Task Because()
        {
            var writer = Start(WriteBeforeClose);
            var removal = Task.CompletedTask;
            try
            {
                await WaitFor(_fenced.Task, writer);
                removal = Start(CloseDropAndRecreate);
                await WaitFor(_closeSubmitted.Task, removal);
                DropWasBlocked = !_dropSubmitted.Task.IsCompleted;
            }
            finally
            {
                _release.TrySetResult();
            }
            await Task.WhenAll(writer, removal).WaitAsync(Timeout);
            await InspectReplacement();
        }

        async Task WriteBeforeClose()
        {
            using var session = await _writerClient.StartSessionAsync();
            Begin(session);
            (await _marker.TryFence(session, _old, CancellationToken.None)).ShouldBeTrue();
            await WriteWitnesses(session);
            _fenced.SetResult();
            await _release.Task.WaitAsync(Timeout);
            await session.CommitTransactionAsync();
            OldCommitAcknowledged = true;
        }
    }
}
