// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelDefinitionExtensions.given;

public class definitions : Specification
{
    protected static ReadModelDefinition Create(string schema = "{\"type\":\"object\",\"properties\":{\"Name\":{\"type\":\"string\"}}}", string index = "Name") => new(
        "some-read-model",
        "some-container",
        "Some read model",
        ReadModelOwner.None,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        "some-projection",
        new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB),
        new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, JsonSchema.FromJson(schema) } },
        [new IndexDefinition(new PropertyPath(index))]);
}
