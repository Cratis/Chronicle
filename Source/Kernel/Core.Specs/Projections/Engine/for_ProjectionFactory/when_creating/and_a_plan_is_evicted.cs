// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_a_plan_is_evicted : given.a_factory_for_two_namespaces
{
    ProjectionPlan _original;
    ProjectionPlan _rebuilt;
    int _afterEviction;

    async Task Because()
    {
        _original = await _factory.GetPlan(_eventStore, _definition, _readModel);
        _factory.Evict(_eventStore, _definition.Identifier);
        _afterEviction = _factory.CachedPlanCount;
        _rebuilt = await _factory.GetPlan(_eventStore, _definition, _readModel);
    }

    [Fact] void should_forget_the_plan() => _afterEviction.ShouldEqual(0);
    [Fact] void should_compile_a_new_plan_on_demand() => _rebuilt.ShouldNotBeSame(_original);
}
