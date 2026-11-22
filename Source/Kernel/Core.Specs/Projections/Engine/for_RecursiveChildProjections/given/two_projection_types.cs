// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_RecursiveChildProjections.given;

public class two_projection_types : Specification
{
    protected static readonly EventType _parentEvent = new("feature-added", 1);
    protected static readonly EventType _childEvent = new("sub-feature-added", 1);
    protected IProjection _parent;
    protected IProjection _child;
    protected JsonSchema _parentSchema;
    protected JsonSchema _childSchema;
    protected IProjection[] _result;

    void Establish()
    {
        _parentSchema = JsonSchema.FromJson("""
            {
                "title": "Feature", "type": "object",
                "properties": {
                    "id": { "type": "string", "format": "uuid" },
                    "children": { "type": "array", "items": { "title": "Feature", "type": "object" } }
                }
            }
            """);
        _childSchema = JsonSchema.FromJson(_parentSchema.ToJson());
        _parent = Substitute.For<IProjection>();
        _child = Substitute.For<IProjection>();
        _parent.TargetReadModelSchema.Returns(_parentSchema);
        _child.TargetReadModelSchema.Returns(_childSchema);
        _parent.ChildProjections.Returns([_child]);
        _parent.OwnEventTypes.Returns([_parentEvent]);
        _child.OwnEventTypes.Returns([_childEvent]);
    }
}
