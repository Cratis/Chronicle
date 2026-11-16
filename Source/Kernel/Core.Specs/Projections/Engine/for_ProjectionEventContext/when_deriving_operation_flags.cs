// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContext;

public class when_deriving_operation_flags : given.a_context
{
    ProjectionEventContext[] _contexts;

    void Because() => _contexts = Enumerable.Range(0, 16).Select(flags => _context with { OperationType = (ProjectionOperationType)flags }).ToArray();

    [Fact] void should_identify_join_operations() => _contexts.Select(_ => _.IsJoin).ShouldEqual([false, false, true, true, false, false, true, true, false, false, true, true, false, false, true, true]);
    [Fact] void should_identify_remove_operations() => _contexts.Select(_ => _.IsRemove).ShouldEqual([false, false, false, false, true, true, true, true, false, false, false, false, true, true, true, true]);
    [Fact] void should_identify_child_operations() => _contexts.Select(_ => _.ChildrenAffected).ShouldEqual([false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true]);
    [Fact] void should_create_instances_only_for_root_non_join_operations() => _contexts.Select(_ => _.CreatesInstance).ShouldEqual([true, true, false, false, true, true, false, false, false, false, false, false, false, false, false, false]);
}
