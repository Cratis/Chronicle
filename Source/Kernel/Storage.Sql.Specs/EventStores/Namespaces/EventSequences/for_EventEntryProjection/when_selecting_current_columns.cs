// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryProjection;

public class when_selecting_current_columns : Specification
{
    string[] _selected;
    string[] _expected;

    void Because()
    {
        var call = (MethodCallExpression)Array.Empty<EventEntry>().AsQueryable().WithoutHistory().Expression;
        var lambda = (LambdaExpression)((UnaryExpression)call.Arguments[1]).Operand;
        var initialization = (MemberInitExpression)lambda.Body;
        _selected = initialization.Bindings.Select(binding => binding.Member.Name).Order().ToArray();
        _expected = typeof(EventEntry).GetProperties().Select(property => property.Name)
            .Except([nameof(EventEntry.OriginalContent), nameof(EventEntry.Revisions)]).Order().ToArray();
    }

    [Fact] void should_select_every_column_except_history() => _selected.ShouldEqual(_expected);
    [Fact] void should_omit_original_content() => _selected.Contains(nameof(EventEntry.OriginalContent)).ShouldBeFalse();
    [Fact] void should_omit_revisions() => _selected.Contains(nameof(EventEntry.Revisions)).ShouldBeFalse();
}
