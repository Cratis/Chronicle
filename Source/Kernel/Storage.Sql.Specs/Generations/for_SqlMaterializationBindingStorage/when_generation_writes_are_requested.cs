// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.Sinks.for_Sinks;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_SqlMaterializationBindingStorage;

public class when_generation_writes_are_requested : Specification
{
    IServiceProvider _services;
    Exception? _error;

    void Establish() => _services = Substitute.For<IServiceProvider>();
    void Because() => _error = Catch.Exception(() => new SinkFactory(_services, Substitute.For<Cratis.Chronicle.Json.IExpandoObjectConverter>()).CreateFor("store", "namespace", GenerationContexts.Create(WellKnownSinkTypes.SQL)));

    [Fact] void should_refuse_before_resolving_a_database_or_allocating_a_table() => _services.DidNotReceive().GetService(Arg.Any<Type>());
    [Fact] void should_report_the_missing_generation_capability() => _error.ShouldBeOfExactType<MaterializationBindingsNotSupported>();
}
