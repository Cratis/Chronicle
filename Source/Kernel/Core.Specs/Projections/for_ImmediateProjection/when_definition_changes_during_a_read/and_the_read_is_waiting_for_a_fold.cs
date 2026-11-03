// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_definition_changes_during_a_read;

public class and_the_read_is_waiting_for_a_fold : given.a_read_waiting_for_a_fold
{
    bool _acknowledgedWhileReading;
    ProjectionDefinition _definitionWhileReading;
    ProjectionResult _result;

    async Task Because()
    {
        _acknowledgedWhileReading = _grain.OnProjectionDefinitionsChanged(_changed).IsCompletedSuccessfully;
        _definitionWhileReading = _grain.Definition;
        _fold.SetResult(_folded);
        _result = await _read;
    }

    [Fact] void should_acknowledge_the_change_without_waiting_for_the_read() => _acknowledgedWhileReading.ShouldBeTrue();
    [Fact] void should_keep_the_reads_definition_while_it_is_active() => _definitionWhileReading.ShouldBeSame(_readDefinition);
    [Fact] void should_complete_the_read_with_its_model() => _result.HasReadModel.ShouldBeTrue();
    [Fact] async Task should_evaluate_visibility_with_the_reads_read_model_definition() => await _visibilities.Received(1).GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Is<ReadModelDefinition>(definition => ReferenceEquals(definition, _readModelDefinition)));
    [Fact] void should_apply_the_change_when_the_read_exits() => _grain.Definition.ShouldBeSame(_changed);
    [Fact] void should_apply_the_change_once() => _grain.AppliedDefinitions.Single().ShouldBeSame(_changed);
}
