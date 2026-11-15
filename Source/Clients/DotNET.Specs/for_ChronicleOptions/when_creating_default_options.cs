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
    [Fact] void should_leave_the_read_model_sink_name_to_the_kernel() => _options.DefaultSinkName.ShouldEqual(string.Empty);
    [Fact] void should_leave_the_graph_sink_name_to_the_kernel() => _options.DefaultGraphSinkName.ShouldEqual(string.Empty);
}
