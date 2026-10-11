// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Reducers.Validators;

namespace Cratis.Chronicle.for_DefaultClientArtifactsProvider;

public class when_type_implements_two_reducer_interfaces : given.an_artifacts_provider
{
    TypeInfo _type;
    Exception _error;

    void Establish()
    {
        _type = TypeImplementingTwice.Of(typeof(IReducerFor<>), typeof(string), typeof(object));
        _definedTypes = [_type];
    }

    void Because() => _error = Catch.Exception(() => _ = _provider.EventTypes);

    [Fact] void should_fail_during_artifact_initialization() => _error.ShouldBeOfExactType<TypeMustImplementOnlyOneReducer>();
    [Fact] void should_name_the_reducer() => _error.Message.ShouldContain(_type.FullName!);
}
