// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.AggregateRoots.ActorBased.Domain;
using Cratis.Chronicle.InProcess.Integration.AggregateRoots.Concepts;
using Cratis.Chronicle.InProcess.Integration.AggregateRoots.Events;
using context = Cratis.Chronicle.InProcess.Integration.AggregateRoots.ActorBased.Scenarios.when_there_are_events_to_rehydrate.and_events_does_not_modify_aggregate_internal_state.context;

namespace Cratis.Chronicle.InProcess.Integration.AggregateRoots.ActorBased.Scenarios.when_there_are_events_to_rehydrate;

[Collection(ChronicleCollection.Name)]
public class and_events_does_not_modify_aggregate_internal_state(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.an_aggregate_root_with_state<IUser, UserInternalState>(chronicleInProcessFixture)
    {
        UserId _userId;

        public bool UserExists;

        void Establish()
        {
            _userId = Guid.NewGuid();
            EventsWithEventSourceIdToAppend.Add(new EventAndEventSourceId(_userId.Value, new UserCreated()));
        }

        Task Because() => DoOnAggregate(_userId.Value, async user => UserExists = await user.Exists());
    }

    [Fact]
    void should_not_be_new_aggregate() => Context.IsNew.ShouldBeFalse();

    [Fact]
    void should_return_that_user_exists() => Context.UserExists.ShouldBeTrue();

    [Fact]
    void should_commit_unit_of_work_successfully() => Context.UnitOfWork.IsSuccess.ShouldBeTrue();

    [Fact]
    void should_not_commit_any_events() => Context.UnitOfWork.GetEvents().ShouldBeEmpty();

    [Fact]
    void should_not_be_deleted() => Context.ResultState.Deleted.ShouldEqual(new StateProperty<bool>(false, 0));

    [Fact]
    void should_not_have_assigned_username() => Context.ResultState.Name.ShouldEqual(new StateProperty<UserName>(default, 0));
}
