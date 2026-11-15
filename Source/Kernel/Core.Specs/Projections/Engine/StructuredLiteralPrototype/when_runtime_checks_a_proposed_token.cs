// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;

namespace Cratis.Chronicle.Projections.Engine.StructuredLiteralPrototype;

/// <summary>
/// Characterizes the CURRENT legacy collision for the proposed #5525 token at runtime value resolution; it does not assert any future behavior.
/// </summary>
public class when_runtime_checks_a_proposed_token : Specification
{
    const string Token = "$structured:v1(e30)";
    const string Wrapped = "$structured:v1($value(42))";

    bool _contentRoot;
    bool _contentNested;
    bool _valueWrapped;
    bool _valueResolversWrapped;

    void Because()
    {
        var content = new EventContentExpressionResolver();
        _contentRoot = content.CanResolve(Token);
        _contentNested = content.CanResolve("Owner." + Token);
        _valueWrapped = new ValueExpressionResolver().CanResolve(Wrapped);
        _valueResolversWrapped = new EventContextPropertyExpressionResolver().CanResolve("$structured:v1($eventContext(Occurred))");
    }

    [Fact] void should_not_resolve_leading_token_as_event_content_in_current_legacy_behavior() => _contentRoot.ShouldBeFalse();

    [Fact] void should_resolve_nested_token_as_event_content_in_current_legacy_behavior() => _contentNested.ShouldBeTrue();

    [Fact] void should_match_value_expression_unanchored_inside_the_token_in_current_legacy_behavior() => _valueWrapped.ShouldBeTrue();

    [Fact] void should_match_event_context_expression_unanchored_inside_the_token_in_current_legacy_behavior() => _valueResolversWrapped.ShouldBeTrue();
}
