// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Concepts;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Domain.Interfaces;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Events;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Scenarios.when_there_are_events_to_rehydrate.and_performing_no_action_on_aggregate.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Scenarios.when_there_are_events_to_rehydrate;

[Collection(GlobalCollection.Name)]
public class and_performing_no_action_on_aggregate(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.context_for_aggregates_with_single_unit_of_work(globalFixture)
    {
        UserId _userId;
        public UserName UserName;

        public IUser User;
        public bool UserExists;
        public bool IsNew;
        public UserInternalState ResultState;

        async Task Establish()
        {
            _userId = Guid.NewGuid();
            UserName = "some name";
            EventsWithEventSourceIdToAppend.Add(new EventAndEventSourceId(_userId.Value, new UserOnBoarded(UserName)));
            User = await AggregateRootFactory.Get<IUser>(_userId.Value);
        }

        async Task Because()
        {
            UserExists = await User.Exists();
            ResultState = await User.GetState();
            IsNew = await User.GetIsNew();
        }
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
    void should_have_assigned_the_username_once() => Context.ResultState.Name.ShouldEqual(new StateProperty<UserName>(Context.UserName, 1));
}
