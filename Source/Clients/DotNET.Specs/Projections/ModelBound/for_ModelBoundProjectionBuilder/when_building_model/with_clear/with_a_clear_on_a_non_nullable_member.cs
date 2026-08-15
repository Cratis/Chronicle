// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_clear;

/// <summary>
/// The ruling: a member that cannot hold null cannot be cleared. Writing its type default instead would be a
/// different fact the read model cannot tell apart from a real value - the sentinel-leaking a scalar clear exists
/// to remove. The build-time analyzer reports the same rule; this is the gate for everything it never sees.
/// </summary>
public class with_a_clear_on_a_non_nullable_member : given.a_model_bound_projection_builder
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => builder.Build(typeof(NonNullableNoteView)));

    [Fact] void should_refuse_to_build_the_projection() => _result.ShouldBeOfExactType<CannotClearNonNullableMember>();
    [Fact] void should_name_the_member_that_cannot_be_cleared() => _result.Message.ShouldContain(nameof(NonNullableNoteView.Note));

    record NonNullableNoteView(
        [Key]
        Guid Id,

        [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.Name))]
        [ClearWith<WithdrawalFromDebitAccountPerformed>]
        string Note);
}
