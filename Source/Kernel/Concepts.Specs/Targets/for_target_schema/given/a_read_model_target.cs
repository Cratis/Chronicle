// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Concepts.Targets.for_target_schema.given;

public class a_read_model_target : Specification
{
    protected ReadModelDefinition _readModel;
    protected IHaveTargetSchema _target;
    protected JsonSchema _firstSchema;
    protected JsonSchema _latestSchema;
    protected JsonSchema _result;

    void Establish()
    {
        _firstSchema = new JsonSchema();
        _latestSchema = new JsonSchema();
        _readModel = new(
            "target",
            "targets",
            "Target",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "projection",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [new ReadModelGeneration(3)] = _latestSchema,
                [ReadModelGeneration.First] = _firstSchema
            },
            []);
        _target = _readModel;
    }
}
