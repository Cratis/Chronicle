// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceNamesFilter.when_executing;

public class and_no_storage_is_registered : given.a_reserved_event_sequence_names_filter
{
    Exception _exception;
    CommandResult _result;

    async Task Because()
    {
        await using var services = new ServiceCollection().AddSingleton(_reservedEventSequenceIds).BuildServiceProvider();
        var filter = new ReservedEventSequenceNamesFilter(services);
        _exception = await Catch.Exception(async () => _result = await filter.OnExecution(ContextFor(AppendTo("orders"))));
    }

    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_return_permission_to_write() => _result.ShouldBeNull();
    [Fact] void should_not_ask_for_reserved_names() => _reservedEventSequenceIds.ReceivedCalls().ShouldBeEmpty();
}
