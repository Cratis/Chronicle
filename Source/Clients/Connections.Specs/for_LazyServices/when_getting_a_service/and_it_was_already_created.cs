// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Chronicle.Connections.for_LazyServices.when_getting_a_service;

public class and_it_was_already_created : given.a_lazy_services
{
    IEventSequences _first;
    IEventSequences _result;

    void Establish() => _first = _services.Sequences;

    void Because() => _result = _services.Sequences;

    [Fact] void should_return_the_same_instance() => ReferenceEquals(_result, _first).ShouldBeTrue();
    [Fact] void should_not_create_it_again() => _factory.Creations.Count.ShouldEqual(1);
}
