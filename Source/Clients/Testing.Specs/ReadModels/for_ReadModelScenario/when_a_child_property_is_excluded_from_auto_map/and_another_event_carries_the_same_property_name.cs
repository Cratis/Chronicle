// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_a_child_property_is_excluded_from_auto_map;

/// <summary>
/// The exclusion has to reach the child, and until it did the value was silently wrong: the attribute compiled,
/// no diagnostic was emitted, and the colliding event auto-mapped over the value the author had sourced
/// explicitly. Nothing distinguishes that from a projection written wrong, so re-reading the attributes to check
/// the exclusion is present and spelled right confirms the wrong conclusion.
/// </summary>
/// <remarks>
/// The bare sibling is the control: it must stay populated, which is what says child auto-mapping is live rather
/// than switched off wholesale. The class-level exclusion did work on a child, but it is blanket - it would blank
/// the sibling too, so one colliding property cost the whole child its mapping.
/// </remarks>
public class and_another_event_carries_the_same_property_name : Specification
{
    ReadModelScenario<FencedBasket> _scenario;
    EventSourceId _basketId;
    Guid _lineId;

    void Establish()
    {
        _scenario = new ReadModelScenario<FencedBasket>();
        _basketId = new EventSourceId(Guid.NewGuid());
        _lineId = Guid.NewGuid();
    }

    async Task Because() => await _scenario.Given
        .ForEventSource(_basketId)
        .Events(
            new FencedBasketOpened("the-basket"),
            new FencedLineAdded(_lineId, "the-original-caption", "the-extra"),
            new FencedLineTouched(_lineId, "the-colliding-caption"));

    FencedLine Line => _scenario.Instance!.Lines.Single();

    [Fact] void should_have_the_child() => _scenario.Instance!.Lines.Count().ShouldEqual(1);
    [Fact] void should_keep_the_explicitly_sourced_value() => Line.Caption.ShouldEqual("the-original-caption");
    [Fact] void should_still_auto_map_the_sibling_property() => Line.Extra.ShouldEqual("the-extra");
}

[EventType]
public record FencedBasketOpened(string Name);

[EventType]
public record FencedLineAdded(Guid Line, string OriginalCaption, string Extra);

[EventType]
public record FencedLineTouched(Guid Line, string Caption);

public record FencedLine(
    Guid Line,

    [SetFrom<FencedLineAdded>(nameof(FencedLineAdded.OriginalCaption))]
    [NoAutoMap]
    string Caption,

    string Extra);

[Passive]
[FromEvent<FencedBasketOpened>]
public record FencedBasket(
    Guid Id,
    string Name,

    [ChildrenFrom<FencedLineAdded>(key: nameof(FencedLineAdded.Line), identifiedBy: nameof(FencedLine.Line))]
    [ChildrenFrom<FencedLineTouched>(key: nameof(FencedLineTouched.Line), identifiedBy: nameof(FencedLine.Line))]
    IEnumerable<FencedLine> Lines);
