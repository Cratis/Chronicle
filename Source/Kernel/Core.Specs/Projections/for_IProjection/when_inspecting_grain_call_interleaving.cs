// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Concurrency;

namespace Cratis.Chronicle.Projections.for_IProjection;

/// <summary>
/// Subscribing to and unsubscribing from definition changes run inside a subscriber's activation and deactivation,
/// during which Orleans dispatches nothing to that subscriber. They must therefore not queue behind a
/// SetDefinition that is notifying that same subscriber. SetDefinition itself stays serialized because it
/// writes state and its notifications must stay ordered, and the other mutating or folding calls stay serialized with it.
/// </summary>
public class when_inspecting_grain_call_interleaving : Specification
{
    bool _subscribingIsInterleaved;
    bool _unsubscribingIsInterleaved;
    bool _gettingTheDefinitionIsInterleaved;
    bool _settingTheDefinitionIsInterleaved;
    bool _removingIsInterleaved;
    bool _foldingIsInterleaved;

    void Because()
    {
        _subscribingIsInterleaved = IsInterleaved(nameof(IProjection.SubscribeDefinitionsChanged));
        _unsubscribingIsInterleaved = IsInterleaved(nameof(IProjection.UnsubscribeDefinitionsChanged));
        _gettingTheDefinitionIsInterleaved = IsInterleaved(nameof(IProjection.GetDefinition));
        _settingTheDefinitionIsInterleaved = IsInterleaved(nameof(IProjection.SetDefinition));
        _removingIsInterleaved = IsInterleaved(nameof(IProjection.Remove));
        _foldingIsInterleaved = IsInterleaved(nameof(IProjection.FoldForSingleReadModel));
    }

    [Fact] void should_interleave_subscribing_to_definition_changes() => _subscribingIsInterleaved.ShouldBeTrue();
    [Fact] void should_interleave_unsubscribing_from_definition_changes() => _unsubscribingIsInterleaved.ShouldBeTrue();
    [Fact] void should_interleave_getting_the_definition() => _gettingTheDefinitionIsInterleaved.ShouldBeTrue();
    [Fact] void should_not_interleave_setting_the_definition() => _settingTheDefinitionIsInterleaved.ShouldBeFalse();
    [Fact] void should_not_interleave_removing_the_projection() => _removingIsInterleaved.ShouldBeFalse();
    [Fact] void should_not_interleave_folding_for_a_single_read_model() => _foldingIsInterleaved.ShouldBeFalse();

    static bool IsInterleaved(string methodName)
    {
        var method = typeof(IProjection).GetMethod(methodName);
        method.ShouldNotBeNull();
        return Attribute.IsDefined(method, typeof(AlwaysInterleaveAttribute));
    }
}
