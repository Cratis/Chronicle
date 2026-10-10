// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.for_DefaultClientArtifactsProvider;

public class when_type_implements_two_projection_interfaces : given.an_artifacts_provider
{
    Exception _error;

    void Establish() => _definedTypes = [typeof(MultipleProjections).GetTypeInfo()];
    void Because() => _error = Catch.Exception(() => _ = _provider.EventTypes);

    [Fact] void should_fail_during_artifact_initialization() => _error.ShouldBeOfExactType<TypeImplementsMultipleProjectionInterfaces>();
    [Fact] void should_name_the_projection() => _error.Message.ShouldContain(typeof(MultipleProjections).FullName!);
    [Fact] void should_name_the_first_read_model() => _error.Message.ShouldContain(typeof(string).FullName!);
    [Fact] void should_name_the_second_read_model() => _error.Message.ShouldContain(typeof(object).FullName!);

    class MultipleProjections : IProjectionFor<string>, IProjectionFor<object>
    {
        public void Define(IProjectionBuilderFor<string> builder) { }
        public void Define(IProjectionBuilderFor<object> builder) { }
    }
}
