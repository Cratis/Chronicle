// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sinks.for_Sinks;

public class when_getting_for_unknown_type : Specification
{
    Sinks _sinks;
    Exception _result;

    void Establish() => _sinks = new(string.Empty, string.Empty, new KnownInstancesOf<ISinkFactory>([]));

    async Task Because() => _result = await Catch.Exception(async () => await _sinks.GetFor(new ReadModelDefinition(
        string.Empty,
        string.Empty,
        string.Empty,
        ReadModelOwner.None,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        ReadModelObserverIdentifier.Unspecified,
        new SinkDefinition(SinkConfigurationId.None, "bc5e82fd-9845-4464-9802-a7e21bd8a919"),
        new Dictionary<ReadModelGeneration, JsonSchema>(),
        [])));

    [Fact] void should_throw_unknown_result_store() => _result.ShouldBeOfExactType<UnknownSink>();
}
