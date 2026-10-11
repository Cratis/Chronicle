// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.for_DefaultClientArtifactsProvider;

public class when_type_implements_one_interface_per_artifact_kind : given.an_artifacts_provider
{
    void Establish() => _definedTypes = [typeof(Artifact).GetTypeInfo()];
    void Because() => _ = _provider.EventTypes;

    [Fact] void should_discover_the_projection() => _provider.Projections.ShouldContainOnly(typeof(Artifact));
    [Fact] void should_discover_the_reducer() => _provider.Reducers.ShouldContainOnly(typeof(Artifact));

    class Artifact : IProjectionFor<string>, IReducerFor<object>
    {
        public void Define(IProjectionBuilderFor<string> builder) { }
    }
}
