// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_graph_artifacts_are_discovered : given.a_catalog
{
    void Establish()
    {
        _discovered.GraphElements.Returns([typeof(AGraphNode), typeof(AGraphNode)]);
        _discovered.GraphProjections.Returns([typeof(AGraphProjection)]);
        _options.RegisterEventType<PlainEvent>();
    }

    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_keep_the_graph_elements() => _catalog.GraphElements.ShouldContainOnly(typeof(AGraphNode));
    [Fact] void should_keep_the_graph_projections() => _catalog.GraphProjections.ShouldContainOnly(typeof(AGraphProjection));

    public class AGraphNode;

    public class AGraphProjection;
}
