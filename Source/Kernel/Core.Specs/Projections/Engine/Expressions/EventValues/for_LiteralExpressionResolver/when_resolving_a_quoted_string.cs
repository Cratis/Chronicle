// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_LiteralExpressionResolver;

public class when_resolving_a_quoted_string : given.an_appended_event
{
    LiteralExpressionResolver _resolver;
    object _result;
    object _numericText;
    object _emptyText;

    void Establish() => _resolver = new();

    void Because()
    {
        _result = _resolver.Resolve("\"draft\"")(@event);
        _numericText = _resolver.Resolve("\"1\"")(@event);
        _emptyText = _resolver.Resolve("\"\"")(@event);
    }

    [Fact] void should_recognize_the_quoted_string() => _resolver.CanResolve("\"draft\"").ShouldBeTrue();
    [Fact] void should_remove_only_the_enclosing_quotes() => _result.ShouldEqual("draft");
    [Fact] void should_keep_quoted_numbers_as_text() => _numericText.ShouldEqual("1");
    [Fact] void should_keep_an_empty_quoted_string() => _emptyText.ShouldEqual(string.Empty);
}
