// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Client = global::Cratis.Chronicle;
using context = Cratis.Chronicle.Kernel.Integration.NamespaceFencing.when_a_staged_unit_commits_after_recreation.context;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing;

[Collection(NamespaceFenceCollection.Name)]
public class when_a_staged_unit_commits_after_recreation(context context) : Given<context>(context)
{
    [Fact] void should_refuse_the_original_commit() => Context.FirstCommitWasRefused.ShouldBeTrue();
    [Fact] void should_keep_the_old_identity_on_commit_retry() => Context.SecondCommitWasRefused.ShouldBeTrue();
    [Fact] void should_not_write_the_staged_decision_into_the_new_lifetime() => Context.CountAfterRefusals.ShouldEqual(0UL);
    [Fact] void should_allow_only_the_new_decision() => Context.CountAfterFreshWrite.ShouldEqual(1UL);

    public class context(NamespaceFenceFixture fixture) : given.a_named_namespace(fixture)
    {
        public bool FirstCommitWasRefused;
        public bool SecondCommitWasRefused;
        public ulong CountAfterRefusals;
        public ulong CountAfterFreshWrite;

        async Task Because()
        {
            _oldUnit.AddEvents(
                Client.EventSequences.EventSequenceId.Log,
                [new Client.EventSequences.EventForEventSourceId("staged-source", new NamespaceFenceValue("old-decision"))],
                new Dictionary<Client.Events.EventSourceId, Client.EventSequences.Concurrency.ConcurrencyScope>
                {
                    ["staged-source"] = new(Client.Events.EventSequenceNumber.Unavailable)
                });
            await Recreate();
            var first = await Catch.Exception(_oldUnit.Commit);
            var second = await Catch.Exception(_oldUnit.Commit);
            FirstCommitWasRefused = IsFenceRefusal(first!);
            SecondCommitWasRefused = IsFenceRefusal(second!);
            CountAfterRefusals = await StoredCount();
            var fresh = await CurrentWriter();
            (await fresh.Append("new-source", new NamespaceFenceValue("new-decision"))).ShouldBeTrue();
            CountAfterFreshWrite = await StoredCount();
        }
    }
}
