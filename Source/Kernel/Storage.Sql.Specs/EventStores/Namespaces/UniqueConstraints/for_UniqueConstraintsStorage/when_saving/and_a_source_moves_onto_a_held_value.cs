// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_saving;

public class and_a_source_moves_onto_a_held_value : given.a_unique_constraints_storage
{
    Exception? _error;
    bool _oldValueIsAllowed;
    bool _heldValueIsAllowed;
    EventSequenceNumber _oldSequenceNumber;

    async Task Establish()
    {
        await _storage.Save("holder", _definition, 42UL, "held");
        await _storage.Save("claimant", _definition, 43UL, "old");
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _storage.Save("claimant", _definition, 44UL, "held"));
        (_oldValueIsAllowed, _oldSequenceNumber) = await _storage.IsAllowed("third-source", _definition, "old");
        (_heldValueIsAllowed, _) = await _storage.IsAllowed("claimant", _definition, "held");
    }

    [Fact] void should_report_the_rejected_move() => _error.ShouldBeOfExactType<DuplicateUniqueConstraintValue>();
    [Fact] void should_keep_the_old_claim() => _oldValueIsAllowed.ShouldBeFalse();
    [Fact] void should_keep_the_old_sequence_number() => _oldSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_keep_the_other_holder() => _heldValueIsAllowed.ShouldBeFalse();
}
