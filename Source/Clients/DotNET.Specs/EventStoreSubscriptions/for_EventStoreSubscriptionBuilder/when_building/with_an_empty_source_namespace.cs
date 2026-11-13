// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptionBuilder.when_building;

public class with_an_empty_source_namespace : given.a_builder
{
    Exception _exception;
    void Because() => _exception = Catch.Exception(() => _builder.WithSourceNamespace(" "));
    [Fact] void should_refuse_the_name() => _exception.ShouldBeOfExactType<ArgumentException>();
}
