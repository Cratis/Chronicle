// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_a_nested_command_has_finished : given.a_contributed_unit
{
    Exception _error;

    void Establish()
    {
        Stage();
        _registration.Dispose();
    }

    void Because() => _error = Catch.Exception(() => Stage(ConcurrencyScope.None));

    [Fact] void should_keep_the_nested_commands_guard() => _error.ShouldBeOfExactType<ConflictingEnrollmentScope>();
    [Fact] void should_keep_only_the_successful_event() => _unitOfWork.GetEvents().ShouldContainOnly(_event);
    [Fact] void should_not_consult_the_disposed_contributor() => _contributor.Received(1).GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>());
}
