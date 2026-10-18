// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_through_definitions;

public class a_legacy_only_batch_for_the_same_id : given.a_definition_backed_batch
{
    async Task Because() => await Append([new(_sourceId, "one") { EventStreamId = "a" }, new(_sourceId, "two") { EventStreamId = "b" }]);

    [Fact] void should_refuse_rather_than_drop_a_guard_on_an_old_kernel() => _exception.ShouldBeOfExactType<Concurrency.IncompatibleConcurrencyScopesForEventSource>();
    [Fact] void should_not_write_anything() => _request.ShouldBeNull();
}
