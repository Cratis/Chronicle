// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_a_scope_only_batch_omits_its_scope : given.a_contributed_unit
{
    void Establish() => Stage();

    async Task Because()
    {
        _unitOfWork.AddEvents(EventSequenceId.Log, [], [new(_source, ConcurrencyScope.NotSet)]);
        await _unitOfWork.Commit();
    }

    [Fact] void should_retain_the_recorded_guard() => _concurrencyScopesAppended[_source].ShouldEqual(_required);
    [Fact] void should_not_consult_for_the_scope_only_entry_or_commit() => _contributor.Received(1).GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>());
}
