// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamIndexUpdater.when_updating;

public class and_event_closes : given.a_closing_index
{
    async Task Because() => await new ClosesStreamIndexUpdater(_definition, ContextFor("Closed"), _storage).Update(new EventSequenceNumber(5));

    [Fact] async Task should_close_the_declared_scope() => (await _storage.GetAll()).Single().Scope.ShouldEqual(_scope);
    [Fact] async Task should_record_the_constraint_owner() => (await _storage.GetAll()).Single().Owner.ShouldEqual(new ClosedStreamOwner("closing"));
    [Fact] async Task should_record_the_durable_event_number() => (await _storage.GetAll()).Single().SequenceNumber.ShouldEqual(new EventSequenceNumber(5));
}
