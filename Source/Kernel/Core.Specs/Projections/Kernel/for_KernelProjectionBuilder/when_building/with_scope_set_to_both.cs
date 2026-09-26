// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Kernel.given;

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionBuilder.when_building;

public class with_scope_set_to_both : Specification
{
    KernelProjectionBuilder<a_read_model> _builder;
    IReadOnlyCollection<ProjectionDefinition> _result;

    void Establish()
    {
        _builder = new(WellKnownKernelProjections.IdentifierFor("stats"), "stats");
        _builder.ScopedTo(ProjectionScope.Both);
    }

    void Because() => _result = _builder.Build();

    [Fact] void should_lower_to_two_single_scope_definitions() => _result.Count.ShouldEqual(2);

    [Fact] void should_not_leave_a_dual_scope_for_the_engine_to_interpret() =>
        _result.All(_ => _.Scope != ProjectionScope.Both).ShouldBeTrue();

    [Fact] void should_build_a_namespaced_half() =>
        _result.Count(_ => _.Scope == ProjectionScope.Namespaced).ShouldEqual(1);

    [Fact] void should_build_a_global_half() =>
        _result.Count(_ => _.Scope == ProjectionScope.Global).ShouldEqual(1);

    [Fact] void should_keep_the_declared_identifier_for_the_namespaced_half() =>
        _result.Single(_ => _.Scope == ProjectionScope.Namespaced).Identifier.Value.ShouldEqual("$system.stats");

    [Fact] void should_distinguish_the_global_half_by_identifier() =>
        _result.Single(_ => _.Scope == ProjectionScope.Global).Identifier.Value.ShouldEqual("$system.stats.global");

    [Fact] void should_materialize_both_halves_into_the_same_read_model() =>
        _result.Select(_ => _.ReadModel.Value).Distinct().Count().ShouldEqual(1);

    [Fact] void should_own_both_halves_by_the_kernel() =>
        _result.All(_ => _.IsKernelOwned).ShouldBeTrue();

    [Fact] void should_make_neither_half_rewindable() =>
        _result.All(_ => !_.IsRewindable).ShouldBeTrue();
}
