// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_RecursiveChildProjections.given;

public class two_schema_references : two_projection_types
{
    protected JsonSchema _document;

    void Establish()
    {
        _document = new JsonSchema(new JsonObject
        {
            ["$defs"] = new JsonObject
            {
                ["Feature"] = _parentSchema.Node.DeepClone(),
                ["OtherFeature"] = _parentSchema.Node.DeepClone()
            }
        });
        _parent.TargetReadModelSchema.Returns(Reference("#/$defs/Feature"));
        _child.TargetReadModelSchema.Returns(Reference("#/$defs/Feature"));
    }

    protected JsonSchema Reference(string target) => new(new JsonObject { ["$ref"] = target }, _document);
}
