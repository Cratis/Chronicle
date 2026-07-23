// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class child_collection_bridged_with_set_from : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(TicketBoardCreated),
            typeof(TicketOpened)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(TicketBoard));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_children_for_tickets() =>
        _result.Children.Keys.ShouldContain(nameof(TicketBoard.Tickets));

    [Fact]
    void should_have_from_definition_for_ticket_opened()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TicketOpened)).ToContract();
        var childrenDef = _result.Children[nameof(TicketBoard.Tickets)];
        childrenDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_map_bridged_child_collection_from_the_differently_named_event_list_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TicketOpened)).ToContract();
        var childrenDef = _result.Children[nameof(TicketBoard.Tickets)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties.Keys.ShouldContain(nameof(Ticket.Comments));
        fromDef.Properties[nameof(Ticket.Comments)].ShouldEqual(nameof(TicketOpened.Annotations));
    }

    [Fact]
    void should_not_explicitly_map_the_same_named_title_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(TicketOpened)).ToContract();
        var childrenDef = _result.Children[nameof(TicketBoard.Tickets)];
        var fromDef = childrenDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;

        // Title has an identically named property on the event, so AutoMap wires it — it must not appear
        // as an explicit client-side mapping.
        fromDef.Properties.Keys.ShouldNotContain(nameof(Ticket.Title));
    }

    [Fact]
    void should_have_auto_map_enabled_on_children()
    {
        var childrenDef = _result.Children[nameof(TicketBoard.Tickets)];
        childrenDef.AutoMap.ShouldEqual(Contracts.Projections.AutoMap.Enabled);
    }
}

public record CommentText(string Value);

[EventType]
public record TicketBoardCreated(TicketBoardId Id);

[EventType]
public record TicketOpened(TicketId TicketId, TicketTitle Title, IReadOnlyList<CommentText> Annotations);

public record TicketBoardId(Guid Value);
public record TicketId(Guid Value);
public record TicketTitle(string Value);

public record Ticket(
    TicketId Id,
    TicketTitle Title,

    [SetFrom<TicketOpened>(nameof(TicketOpened.Annotations))]
    IReadOnlyList<CommentText> Comments);

[FromEvent<TicketBoardCreated>]
public record TicketBoard(
    TicketBoardId Id,

    [ChildrenFrom<TicketOpened>(key: nameof(TicketOpened.TicketId), identifiedBy: nameof(Ticket.Id))]
    IEnumerable<Ticket> Tickets);
