// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Concepts;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Domain.Interfaces;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Scenarios.when_there_are_no_events_to_rehydrate.and_events_does_not_modify_aggregate_internal_state.context;
namespace Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Scenarios.when_there_are_no_events_to_rehydrate;

[Collection(GlobalCollection.Name)]
public class and_events_does_not_modify_aggregate_internal_state(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.context_for_aggregate_root<IUser, UserInternalState>(globalFixture)
    {
        UserId _userId;

        public bool UserExists;

        void Establish()
        {
            _userId = Guid.NewGuid();
        }

        Task Because() => DoOnAggregate(_userId.Value, async user =>
        {
            await user.Create();
            UserExists = await user.Exists();
        });
    }

    [Fact]
    void should_not_be_new_aggregate() => Context.IsNew.ShouldBeFalse();

    [Fact]
    void should_return_that_user_exists() => Context.UserExists.ShouldBeTrue();

    [Fact]
    void should_commit_unit_of_work_successfully() => Context.UnitOfWork.IsSuccess.ShouldBeTrue();

    [Fact]
    void should_commit_user_created_event() => Context.UnitOfWork.GetEvents().ShouldContainSingleItem();

    [Fact]
    void should_not_be_deleted() => Context.ResultState.Deleted.ShouldEqual(new StateProperty<bool>(false, 0));

    [Fact]
    void should_not_have_assigned_username() => Context.ResultState.Name.ShouldEqual(new StateProperty<UserName>(default, 0));
}
