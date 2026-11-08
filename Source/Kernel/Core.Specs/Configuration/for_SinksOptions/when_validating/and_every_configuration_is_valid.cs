// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_SinksOptions.when_validating;

public class and_every_configuration_is_valid : Specification
{
    SinksOptions _options;
    Exception _error;

    void Establish() => _options = new()
    {
        ReadModels = "primary",
        Graphs = "people",
        Configurations = new Dictionary<string, SinkConfiguration>(StringComparer.OrdinalIgnoreCase)
        {
            ["primary"] = new() { Type = "MongoDB", ConnectionDetails = "mongodb://mongo:27017" },
            ["reporting"] = new() { Type = "SQL", ConnectionDetails = "Host=pg;Database=reporting" },
            ["people"] = new() { Type = "Neo4j", Server = "bolt://neo4j:7687" }
        }
    };

    void Because() => _error = Catch.Exception(_options.Validate);

    [Fact] void should_accept_it() => _error.ShouldBeNull();
}
