// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Chronicle.Connections.for_LazyServices.when_getting_a_service;

public class and_creating_it_fails : given.a_lazy_services
{
    readonly Exception _failure = new InvalidOperationException("Proxy creation failed");
    Exception _error;
    IEventSequences _result;

    void Establish()
    {
        _factory.FailingServiceType = typeof(IEventSequences);
        _factory.Failure = _failure;
    }

    void Because()
    {
        _error = Catch.Exception(() => _ = _services.Sequences);
        _factory.FailingServiceType = null;
        _result = _services.Sequences;
    }

    [Fact] void should_throw_the_creation_error() => ReferenceEquals(_error, _failure).ShouldBeTrue();
    [Fact] void should_create_it_on_the_next_access() => ReferenceEquals(_result, _factory.Creations.Single().Service).ShouldBeTrue();
}
