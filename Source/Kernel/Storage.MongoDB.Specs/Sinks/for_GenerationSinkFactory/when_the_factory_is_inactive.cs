// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_GenerationSinkFactory;

public class when_the_factory_is_inactive : Specification
{
    IServiceProvider _services;
    Exception? _error;

    void Establish() => _services = Substitute.For<IServiceProvider>();
    void Because() => _error = Catch.Exception(() => _ = new SinkFactory(_services, Substitute.For<ITypeFormats>()));

    [Fact] void should_construct_without_mongodb_services() => _error.ShouldBeNull();
    [Fact] void should_not_resolve_generation_or_database_services() => _services.DidNotReceive().GetService(Arg.Any<Type>());
}
