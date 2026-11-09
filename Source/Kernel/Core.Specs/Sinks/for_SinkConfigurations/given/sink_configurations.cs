// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Sinks.for_SinkConfigurations.given;

public class sink_configurations : Specification
{
    protected SinkConfigurations _configurations;

    protected virtual SinksOptions Sinks => new()
    {
        Configurations = new Dictionary<string, SinkConfiguration>(StringComparer.OrdinalIgnoreCase)
        {
            ["Primary"] = new() { Type = "MongoDB", ConnectionDetails = "mongodb://mongo:27017" },
            ["Reporting"] = new() { Type = "sql", ConnectionDetails = "Host=pg;Database=reporting" },
            ["People"] = new() { Type = "Neo4j", Server = "bolt://neo4j:7687" }
        }
    };

    void Establish() => _configurations = new(Options.Create(new ChronicleOptions { Sinks = Sinks }));

    protected static ReadModelDefinition ReadModel(SinkTypeId type, SinkName? name = null) => new(
        "orders",
        "orders",
        "Orders",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        "projection",
        new SinkDefinition(SinkConfigurationId.None, type) { Name = name ?? SinkName.Default },
        new Dictionary<ReadModelGeneration, JsonSchema>(),
        []);
}
