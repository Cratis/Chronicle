// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptions.when_registering;

public class with_source_namespaces : given.subscriptions
{
    async Task Because() => await _subscriptions.Subscribe("s", "source", builder => builder.WithEventType<CustomerRegistered>().WithSourceNamespace("Default"));
    [Fact] void should_send_the_source_namespaces() => _request.SourceNamespaces.ShouldContainOnly("Default");
}
