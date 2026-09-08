// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.for_TypeUniverse;

public class when_getting_derived_types_for_the_current_universe : Specification
{
    IDerivedTypes _result;
    IDerivedTypes _resultOfSecondCall;

    void Because()
    {
        _result = TypeUniverse.CurrentDerivedTypes();
        _resultOfSecondCall = TypeUniverse.CurrentDerivedTypes();
    }

    [Fact] void should_not_be_the_static_snapshot() => ReferenceEquals(_result, DerivedTypes.Instance).ShouldBeFalse();
    [Fact] void should_reuse_the_instance_built_for_the_same_universe() => ReferenceEquals(_result, _resultOfSecondCall).ShouldBeTrue();
}
