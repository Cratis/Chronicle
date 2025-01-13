// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Concepts;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Domain.Interfaces;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Events;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Scenarios.when_there_are_no_events_to_rehydrate.and_performing_no_action_on_aggregate.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Scenarios.when_there_are_no_events_to_rehydrate;

[Collection(GlobalCollection.Name)]
public class and_performing_no_action_on_aggregate(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.context_for_aggregate_root<IUser, UserInternalState>(globalFixture)
    {
        UserId _userId;

        public bool UserExists;

        void Establish()
        {
            _userId = Guid.NewGuid();
        }

        Task Because() => DoOnAggregate(_userId.Value, async user => UserExists = await user.Exists());
    }

    [Fact]
    void should_be_new_aggregate() => Context.IsNew.ShouldBeTrue();

    [Fact]
    void should_return_that_user_does_not_exist() => Context.UserExists.ShouldBeFalse();

    [Fact]
    void should_commit_unit_of_work_successfully() => Context.UnitOfWork.IsSuccess.ShouldBeTrue();

    [Fact]
    void should_not_commit_any_events() => Context.UnitOfWork.GetEvents().ShouldBeEmpty();

    [Fact]
    void should_not_be_deleted() => Context.ResultState.Deleted.ShouldEqual(new StateProperty<bool>(false, 0));
}