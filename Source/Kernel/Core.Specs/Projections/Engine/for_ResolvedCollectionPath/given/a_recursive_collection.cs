// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ResolvedCollectionPath.given;

public class a_recursive_collection : Specification
{
    protected IProjection _parent;
    protected IProjection _recursive;
    protected IProjection _slices;
    protected Key _parentKey;
    protected PropertyPath _path;

    void Establish()
    {
        var document = JsonSchema.FromJson("""
            {
                "type": "object",
                "properties": { "features": { "type": "array", "items": { "$ref": "#/$defs/Feature" } } },
                "$defs": {
                    "Feature": {
                        "title": "Feature", "type": "object",
                        "properties": {
                            "id": { "type": "string", "format": "uuid" },
                            "subFeatures": { "type": "array", "items": { "$ref": "#/$defs/Feature" } },
                            "slices": { "type": "array", "items": { "title": "Slice", "type": "object" } }
                        }
                    }
                }
            }
            """);
        var schema = document.Properties["features"].Item!.ActualSchema;
        _parent = Substitute.For<IProjection>();
        _parent.ChildrenPropertyPath.Returns((PropertyPath)"features");
        _parent.TargetReadModelSchema.Returns(schema);
        _recursive = Substitute.For<IProjection>();
        _recursive.HasParent.Returns(true);
        _recursive.Parent.Returns(_parent);
        _recursive.ChildrenPropertyPath.Returns((PropertyPath)"features.subFeatures");
        _recursive.TargetReadModelSchema.Returns(schema.Properties["subFeatures"].Item!.ActualSchema);
        _parent.ChildProjections.Returns([_recursive]);
        _slices = Substitute.For<IProjection>();
        _slices.HasParent.Returns(true);
        _slices.Parent.Returns(_parent);
        _slices.ChildrenPropertyPath.Returns((PropertyPath)"features.slices");
        _parentKey = new Key("root", new ArrayIndexers(
            [new("features", "id", "feature"), new("features.subFeatures", "id", "sub-feature")]));
    }
}
