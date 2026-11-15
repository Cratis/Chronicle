// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_StoreClientArtifactsProvider.when_asking_for_graph_artifacts;

public class after_the_store_is_prepared : Specification
{
    IClientArtifactsProvider _native;
    StoreClientArtifactsProvider _provider;

    void Establish()
    {
        _native = Substitute.For<IClientArtifactsProvider>();
        _native.GraphElements.Returns([typeof(AGraphNode)]);
        _native.GraphProjections.Returns([typeof(AGraphProjection)]);
        _provider = new(_native, new ExplicitArtifacts().CreateSnapshot());
    }

    void Because() => _provider.Prepare();

    [Fact] void should_keep_the_graph_elements_of_the_native_provider() => _provider.GraphElements.ShouldContainOnly(typeof(AGraphNode));
    [Fact] void should_keep_the_graph_projections_of_the_native_provider() => _provider.GraphProjections.ShouldContainOnly(typeof(AGraphProjection));

    public class AGraphNode;

    public class AGraphProjection;
}
