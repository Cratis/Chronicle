// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_the_definition_or_read_model_changes_after_a_plan_was_compiled : given.a_factory_for_two_namespaces
{
    ProjectionPlan _original;
    ProjectionPlan _sameAgain;
    ProjectionPlan _changedDefinition;
    ProjectionPlan _changedReadModel;

    async Task Because()
    {
        _original = await _factory.GetPlan(_eventStore, _definition, _readModel);
        _sameAgain = await _factory.GetPlan(_eventStore, _definition, _readModel);
        _changedDefinition = await _factory.GetPlan(_eventStore, _definition with { IsRewindable = !_definition.IsRewindable }, _readModel);
        _changedReadModel = await _factory.GetPlan(_eventStore, _definition, _readModel with { ContainerName = new("other-container") });
    }

    [Fact] void should_reuse_the_plan_for_the_same_input() => _sameAgain.ShouldBeSame(_original);
    [Fact] void should_compile_another_plan_for_a_changed_definition() => _changedDefinition.ShouldNotBeSame(_original);
    [Fact] void should_compile_another_plan_for_a_changed_read_model() => _changedReadModel.ShouldNotBeSame(_original);
    [Fact] void should_keep_the_plans_of_the_current_and_the_previous_definition_only() => _factory.CachedPlanCount.ShouldBeLessThanOrEqual(2);
}
