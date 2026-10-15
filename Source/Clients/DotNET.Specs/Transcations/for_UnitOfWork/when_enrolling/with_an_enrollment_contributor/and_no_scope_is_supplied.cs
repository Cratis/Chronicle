// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_no_scope_is_supplied : given.a_contributed_unit
{
    bool _stagedAtNotification;

    void Establish() => _contributor.When(_ => _.Enrolled(Arg.Any<UnitOfWorkEnrollment>(), _required))
        .Do(_ => _stagedAtNotification = _unitOfWork.GetEvents().Contains(_event));

    async Task Because()
    {
        Stage();
        await _unitOfWork.Commit();
    }

    [Fact] void should_commit_with_the_required_scope() => _concurrencyScopesAppended[_source].ShouldEqual(_required);
    [Fact] void should_notify_only_after_staging() => _stagedAtNotification.ShouldBeTrue();
    [Fact] void should_describe_the_enrollment() => _contributor.Received(1).GetRequiredScope(Arg.Is<UnitOfWorkEnrollment>(_ =>
        _.EventSourceId == _source && _.EventSequenceId == EventSequenceId.Log && _.Events.Single() == _event));
}
