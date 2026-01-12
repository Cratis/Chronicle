// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.for_Projection.given;

public class a_projection : Specification
{
    protected Projection projection;
    protected IKeyResolvers keyResolvers;

    void Establish()
    {
        projection = new Projection(
            EventSequenceId.Log,
            "0b7325dd-7a25-4681-9ab7-c387a6073547",
            new ExpandoObject(),
            string.Empty,
            string.Empty,
            string.Empty,
            new ReadModelDefinition(string.Empty, string.Empty, string.Empty, ReadModelOwner.None, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema>(), []),
            new JsonSchema(),
            true,
            AutoMap.Enabled,
            []);
        keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
    }
}
