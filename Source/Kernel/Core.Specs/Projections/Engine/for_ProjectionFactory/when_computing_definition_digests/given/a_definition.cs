// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_computing_definition_digests.given;

public class a_definition : when_creating.given.a_child_collection_projection
{
    protected readonly EventType _first = new("first", 1);
    protected readonly EventType _second = new("second", 2);
    protected ProjectionDefinition _other;
    protected ProjectionDefinitionDigest _digest;
    protected ProjectionDefinitionDigest _otherDigest;

    void Establish()
    {
        _definition = _definition with { Children = new Dictionary<PropertyPath, ChildrenDefinition>() };
        _definition.From[_first] = new(new Dictionary<PropertyPath, string> { ["name"] = "name" }, "$eventSourceId", null);
        _definition.From[_second] = new(new Dictionary<PropertyPath, string> { ["value"] = "value" }, "$eventSourceId", null);
        _other = _definition;
    }

    protected void Compute()
    {
        _digest = ProjectionFactory.DefinitionDigestFor(_definition);
        _otherDigest = ProjectionFactory.DefinitionDigestFor(_other);
    }
}
