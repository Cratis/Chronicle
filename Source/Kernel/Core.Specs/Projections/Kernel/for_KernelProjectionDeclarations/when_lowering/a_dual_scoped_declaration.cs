// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionDeclarations.when_lowering;

/// <summary>
/// Both is two definitions rather than one the engine materializes twice - a projection is observed per namespace,
/// so a dual scope is two observers by nature. They share a read model, and must not share an identifier.
/// </summary>
public class a_dual_scoped_declaration : Specification
{
    ReadModelDefinition _readModel;
    IReadOnlyCollection<ProjectionDefinition> _projections;

    void Because() => (_readModel, _projections) = KernelProjectionDeclarations.Lower(typeof(given.a_dual_scoped_declaration));

    [Fact] void should_lower_to_two_definitions() => _projections.Count.ShouldEqual(2);
    [Fact] void should_have_one_of_each_scope() => _projections.Select(_ => _.Scope).ShouldContainOnly([ProjectionScope.Namespaced, ProjectionScope.Global]);
    [Fact] void should_give_them_distinct_identifiers() => _projections.Select(_ => _.Identifier).Distinct().Count().ShouldEqual(2);
    [Fact] void should_suffix_the_global_half() => _projections.Single(_ => _.Scope == ProjectionScope.Global).Identifier.Value.ShouldEqual("$system.a-dual-scoped-one.global");
    [Fact] void should_leave_the_namespaced_half_unsuffixed() => _projections.Single(_ => _.Scope == ProjectionScope.Namespaced).Identifier.Value.ShouldEqual("$system.a-dual-scoped-one");
    [Fact] void should_write_both_halves_to_the_same_read_model() => _projections.Select(_ => _.ReadModel).Distinct().Count().ShouldEqual(1);
    [Fact] void should_write_to_the_declared_read_model() => _projections.First().ReadModel.ShouldEqual(_readModel.Identifier);
    [Fact] void should_own_both_halves_by_the_kernel() => _projections.ShouldContainOnly(_projections.Where(p => p.Owner == ProjectionOwner.Kernel));
    [Fact] void should_make_neither_half_rewindable() => _projections.ShouldContainOnly(_projections.Where(p => !p.IsRewindable));
}
