// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_two_stores_create_the_same_namespace : given.a_namespaces_grain
{
    Namespaces _otherStore;

    async Task Establish()
    {
        var silo = new TestKitSilo();
        silo.AddService(_client);
        silo.AddService(NullLogger<Namespaces>.Instance);
        silo.AddProbe<IEventSequence>(_ => _systemSequence);
        _otherStore = await silo.CreateGrainAsync<Namespaces>("other-store");
    }

    async Task Because()
    {
        await _namespaces.Ensure(_namespace);
        await _otherStore.Ensure(_namespace);
    }

    [Fact] void should_use_distinct_reactor_partitions() => _systemSequence.ReceivedCalls()
        .Where(_ => _.GetMethodInfo().Name == nameof(IEventSequence.Append))
        .Select(_ => (EventSourceId)_.GetArguments()[0]!)
        .Distinct().Count().ShouldEqual(2);
    [Fact] async Task should_scope_the_first_namespace_to_its_store() => await _systemSequence.Received(1).Append($"{_eventStore}/{_namespace}", new NamespaceAdded(_eventStore, _namespace));
    [Fact] async Task should_scope_the_second_namespace_to_its_store() => await _systemSequence.Received(1).Append($"other-store/{_namespace}", new NamespaceAdded("other-store", _namespace));
}
