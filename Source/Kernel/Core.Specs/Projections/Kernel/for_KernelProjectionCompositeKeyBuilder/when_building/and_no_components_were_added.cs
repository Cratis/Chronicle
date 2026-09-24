// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Kernel.given;

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionCompositeKeyBuilder.when_building;

public class and_no_components_were_added : Specification
{
    KernelProjectionCompositeKeyBuilder<a_read_model> _builder;
    Exception _result;

    void Establish() => _builder = new();

    void Because() => _result = Catch.Exception(() => _builder.Build());

    [Fact] void should_fail_rather_than_collapse_every_instance_onto_one() =>
        _result.ShouldBeOfExactType<KernelProjectionCompositeKeyHasNoComponents>();
}
