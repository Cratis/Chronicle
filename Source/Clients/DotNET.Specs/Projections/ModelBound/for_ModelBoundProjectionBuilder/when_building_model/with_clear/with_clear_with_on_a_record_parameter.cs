// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_clear;

/// <summary>
/// The declaration that used to bind to nothing at all: a [ClearWith] on a scalar member. It has to produce a real
/// mapping, because a member with no mapping keeps whatever it last held.
/// </summary>
public class with_clear_with_on_a_record_parameter : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(AccountNoteView));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_a_clear_expression_for_the_note_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WithdrawalFromDebitAccountPerformed)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Properties[nameof(AccountNoteView.Note)].ShouldEqual(WellKnownExpressions.Null);
    }

    record AccountNoteView(
        [Key]
        Guid Id,

        [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
        [ClearWith<WithdrawalFromDebitAccountPerformed>]
        string? Note);
}
