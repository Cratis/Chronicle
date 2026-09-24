// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Kernel.given;

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionBuilder.when_building;

public class with_scope_set_to_both : Specification
{
    KernelProjectionBuilder<a_read_model> _builder;
    ProjectionDefinition _result;

    void Establish()
    {
        _builder = new(WellKnownKernelProjections.IdentifierFor("stats"), "stats");
        _builder.ScopedTo(ProjectionScope.Both);
    }

    void Because() => _result = _builder.Build();

    [Fact] void should_carry_the_scope_on_the_definition() => _result.Scope.ShouldEqual(ProjectionScope.Both);
}
