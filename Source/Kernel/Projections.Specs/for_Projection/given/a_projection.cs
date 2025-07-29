// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.EventSequences;
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
            SinkDefinition.None,
            new ExpandoObject(),
            string.Empty,
            string.Empty,
            new ReadModelDefinition(string.Empty, ReadModelOwner.None, new Dictionary<ReadModelGeneration, JsonSchema>()),
            new JsonSchema(),
            true,
            []);
        keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
    }
}
