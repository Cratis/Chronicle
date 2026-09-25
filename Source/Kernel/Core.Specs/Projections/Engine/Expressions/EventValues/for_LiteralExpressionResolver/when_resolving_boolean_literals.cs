// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_LiteralExpressionResolver;

public class when_resolving_boolean_literals : given.an_appended_event
{
    LiteralExpressionResolver _resolver;
    object _true;
    object _false;
    object _lowercase;

    void Establish() => _resolver = new();

    void Because()
    {
        _true = _resolver.Resolve("True")(@event);
        _false = _resolver.Resolve("False")(@event);
        _lowercase = _resolver.Resolve("true")(@event);
    }

    [Fact] void should_recognize_stored_boolean_values() => _resolver.CanResolve("False").ShouldBeTrue();
    [Fact] void should_return_true_as_a_boolean() => _true.ShouldEqual(true);
    [Fact] void should_return_false_as_a_boolean() => _false.ShouldEqual(false);
    [Fact] void should_accept_lowercase_boolean_values() => _lowercase.ShouldEqual(true);
}
