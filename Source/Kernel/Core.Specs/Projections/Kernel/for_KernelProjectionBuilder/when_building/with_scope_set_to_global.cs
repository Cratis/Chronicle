// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Kernel.given;

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionBuilder.when_building;

public class with_scope_set_to_global : Specification
{
    KernelProjectionBuilder<a_read_model> _builder;
    Exception _error;

    void Establish()
    {
        _builder = new(WellKnownKernelProjections.IdentifierFor("stats"), "stats");
        _builder.ScopedTo(ProjectionScope.Global);
    }

    void Because() => _error = Catch.Exception(() => _builder.Build());

    [Fact] void should_refuse_global_materialization() => _error.ShouldNotBeNull();
    [Fact] void should_explain_the_generation_boundary() => _error.Message.ShouldContain("0014");
}
