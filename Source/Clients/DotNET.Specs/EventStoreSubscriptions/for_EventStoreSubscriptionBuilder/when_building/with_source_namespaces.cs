// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder.when_building;

public class with_source_namespaces : given.a_builder
{
    void Establish() => _builder.WithSourceNamespace("Default").WithSourceNamespaces("Tenant1", "default", "TENANT1");
    void Because() => _result = _builder.Build();
    [Fact] void should_keep_each_distinct_namespace_once() => _result.SourceNamespaces.ShouldContainOnly("Default", "Tenant1");
}
