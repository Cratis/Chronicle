// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Services.Sinks.for_SinkDefinitionConverters.when_reading_with_a_legacy_contract;

public class and_there_is_no_name : Specification
{
    SinkDefinition _result;

    void Because() => _result = new Contracts.Sinks.SinkDefinition { ConfigurationId = Guid.Empty, TypeId = WellKnownSinkTypes.MongoDB.Value, Name = null! }.ToChronicle();

    [Fact] void should_store_to_the_default_sink() => _result.Name.ShouldEqual(SinkName.Default);
}
