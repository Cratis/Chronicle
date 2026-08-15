// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_clear;

/// <summary>
/// A null [SetValue] used to be dropped where the set-value mappings are built, leaving the member unmapped while
/// still counting as declared. It is the same clear as [ClearWith] and produces the same expression. The bare null
/// also has to compile - the attribute parameter is nullable, so no null-forgiving operator appears here.
/// </summary>
public class with_a_null_set_value : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Because() => _result = builder.Build(typeof(AccountNoteBySetValueView));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();

    [Fact]
    void should_have_a_clear_expression_for_the_note_property()
    {
        var eventType = event_types.GetEventTypeFor(typeof(WithdrawalFromDebitAccountPerformed)).ToContract();
        var fromDefinition = _result.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDefinition.Properties[nameof(AccountNoteBySetValueView.Note)].ShouldEqual(WellKnownExpressions.Null);
    }

    record AccountNoteBySetValueView(
        [Key]
        Guid Id,

        [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
        [SetValue<WithdrawalFromDebitAccountPerformed>(null)]
        string? Note);
}
