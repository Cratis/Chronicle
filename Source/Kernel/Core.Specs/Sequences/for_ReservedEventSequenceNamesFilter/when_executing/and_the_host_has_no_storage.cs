// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceNamesFilter.when_executing;

public class and_the_host_has_no_storage : given.a_reserved_event_sequence_names_filter
{
    CommandResult _result;

    async Task Because()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        _result = await new ReservedEventSequenceNamesFilter(services).OnExecution(ContextFor(new object()));
    }

    [Fact] void should_return_no_result() => _result.ShouldBeNull();
}
