// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.for_DefaultClientArtifactsProvider;

public class when_type_inherits_one_projection_interface_and_declares_another : given.an_artifacts_provider
{
    TypeInfo _type;
    Exception _error;

    void Establish()
    {
        _type = TypeImplementingTwice.ThroughInheritance(typeof(IProjectionFor<>), typeof(string), typeof(object));
        _definedTypes = [_type];
    }

    void Because() => _error = Catch.Exception(() => _ = _provider.EventTypes);

    [Fact] void should_fail_during_artifact_initialization() => _error.ShouldBeOfExactType<TypeImplementsMultipleProjectionInterfaces>();
    [Fact] void should_name_the_projection() => _error.Message.ShouldContain(_type.FullName!);
    [Fact] void should_name_the_first_read_model() => _error.Message.ShouldContain(typeof(string).FullName!);
    [Fact] void should_name_the_second_read_model() => _error.Message.ShouldContain(typeof(object).FullName!);
}
