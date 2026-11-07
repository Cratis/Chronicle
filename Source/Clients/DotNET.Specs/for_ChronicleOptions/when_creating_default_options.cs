// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.for_ChronicleOptions;

public class when_creating_default_options : Specification
{
    ChronicleOptions _options;

    void Because() => _options = new ChronicleOptions();

    [Fact] void should_store_graphs_in_neo4j() => _options.DefaultGraphSinkTypeId.ShouldEqual(WellKnownSinkTypes.Neo4j);
    [Fact] void should_keep_the_document_sink_default() => _options.DefaultSinkTypeId.ShouldEqual(WellKnownSinkTypes.MongoDB);
}
