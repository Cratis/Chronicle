// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.InProcess.Integration.AggregateRoots.Concepts;
using Cratis.Chronicle.InProcess.Integration.AggregateRoots.Domain;
using Cratis.Chronicle.InProcess.Integration.AggregateRoots.Events;
using context = Cratis.Chronicle.InProcess.Integration.AggregateRoots.Scenarios.when_concurrent_changes_are_made_by_others.context;

namespace Cratis.Chronicle.InProcess.Integration.AggregateRoots.Scenarios;

[Collection(ChronicleCollection.Name)]
public class when_concurrent_changes_are_made_by_others(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.an_aggregate_root_with_state<User, UserInternalState>(chronicleInProcessFixture)
    {
        UserId _userId;
        public AggregateRootCommitResult Result;

        void Establish()
        {
            _userId = Guid.NewGuid();
        }

        async Task Because()
        {
            var user = await AggregateRootFactory.Get<User>(_userId.Value);
            await EventStore.EventLog.Append(_userId.Value, new UserOnBoarded("Something"), eventStreamType: user.GetEventStreamType());
            await user.ChangeUserName("ConcurrentChange");
            Result = await user.Commit();
        }
    }

    [Fact] void should_fail_committing_unit_of_work() => Context.Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_concurrency_violation() => Context.Result.ConcurrencyViolations.Count().ShouldEqual(1);
}
