// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_ensuring_a_new_namespace : given.a_namespaces_grain
{
    async Task Because() => await new EnsureNamespace(_eventStore, _namespace).Handle(GrainFactory());

    IGrainFactory GrainFactory()
    {
        var factory = Substitute.For<IGrainFactory>();
        factory.GetGrain<INamespaces>(_eventStore.Value, default).Returns(_namespaces);
        return factory;
    }

    [Fact] async Task should_append_the_namespace_added_event_for_the_system_reactor() =>
        await _systemSequence.Received(1).Append($"{_eventStore}/{_namespace}", new NamespaceAdded(_eventStore, _namespace));
    [Fact] async Task should_keep_the_namespace_added_broadcast() =>
        await _writer.Received(1).Publish(new NamespaceAdded(_eventStore, _namespace));
}
