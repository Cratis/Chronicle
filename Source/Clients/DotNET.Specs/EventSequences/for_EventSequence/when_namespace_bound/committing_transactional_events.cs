// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_namespace_bound;

public class committing_transactional_events : given.a_namespace_bound_append
{
    async Task Because()
    {
        var unit = CreateUnitOfWork();
        _unitOfWorkManager.Current.Returns(unit);
        await _eventSequence.Transactional.Append("source", "event");
        await unit.Commit();
    }

    [Fact] void should_pin_the_transaction_to_the_writer_identity() => _boundBatch.ExpectedIncarnation.ShouldEqual(_binding.Incarnation.Value);
    [Fact] void should_not_downgrade() => ShouldNotUseLegacyAppendRoutes();
}
