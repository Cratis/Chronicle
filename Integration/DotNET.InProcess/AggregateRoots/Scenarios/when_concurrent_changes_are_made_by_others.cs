// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.AggregateRoots.Concepts;
using Cratis.Chronicle.InProcess.Integration.AggregateRoots.Domain.Interfaces;
using Cratis.Chronicle.InProcess.Integration.AggregateRoots.Events;
using context = Cratis.Chronicle.InProcess.Integration.AggregateRoots.Scenarios.when_concurrent_changes_are_made_by_others.context;

namespace Cratis.Chronicle.InProcess.Integration.AggregateRoots.Scenarios;

[Collection(ChronicleCollection.Name)]
public class when_concurrent_changes_are_made_by_others(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.context_for_aggregate_root<IUser, UserInternalState>(chronicleInProcessFixture)
    {
        UserId _userId;

        void Establish()
        {
            _userId = Guid.NewGuid();
            EventsWithEventSourceIdToAppend.Add(new EventAndEventSourceId(_userId.Value, new UserCreated()));
        }

        async Task Because()
        {
            var user = await AggregateRootFactory.Get<IUser>(_userId.Value);
            await user.ChangeUserName("ConcurrentChange");

            var correlationId = await user.GetCorrelationId();
            if (UnitOfWorkManager.TryGetFor(correlationId, out UnitOfWork))
            {
                await UnitOfWork.Commit();
            }
        }
    }

    [Fact] void should_fail_committing_unit_of_work() => Context.UnitOfWork.IsSuccess.ShouldBeFalse();
}
