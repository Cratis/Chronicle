// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_clear;

/// <summary>
/// The property form goes through a different pass of the builder than the record-parameter form, so it is pinned
/// separately rather than assumed to follow.
/// </summary>
public class with_clear_with_on_a_property : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(AccountNotePropertyView));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_a_clear_expression_for_the_note_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WithdrawalFromDebitAccountPerformed)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Properties[nameof(AccountNotePropertyView.Note)].ShouldEqual(WellKnownExpressions.Null);
    }

    class AccountNotePropertyView
    {
        [Key]
        public Guid Id { get; set; }

        [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
        [ClearWith<WithdrawalFromDebitAccountPerformed>]
        public string? Note { get; set; }
    }
}
