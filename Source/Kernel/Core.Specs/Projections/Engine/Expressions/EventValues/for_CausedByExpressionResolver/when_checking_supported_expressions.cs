// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_CausedByExpressionResolver;

public class when_checking_supported_expressions : Specification
{
    bool _acceptsIdentity;
    bool _acceptsProperty;
    bool _acceptsUnknownProperty;
    bool _acceptsTrailingText;
    bool _acceptsAnotherExpression;

    void Because()
    {
        var resolver = new CausedByExpressionResolver();
        _acceptsIdentity = resolver.CanResolve(WellKnownExpressions.CausedBy);
        _acceptsProperty = resolver.CanResolve($"{WellKnownExpressions.CausedBy}(subject)");
        _acceptsUnknownProperty = resolver.CanResolve($"{WellKnownExpressions.CausedBy}(bogus)");
        _acceptsTrailingText = resolver.CanResolve($"{WellKnownExpressions.CausedBy}(subject)extra");
        _acceptsAnotherExpression = resolver.CanResolve($"{WellKnownExpressions.EventContext}(causedBy.subject)");
    }

    [Fact] void should_accept_the_identity() => _acceptsIdentity.ShouldBeTrue();
    [Fact] void should_accept_a_property() => _acceptsProperty.ShouldBeTrue();
    [Fact] void should_reject_an_unknown_property() => _acceptsUnknownProperty.ShouldBeFalse();
    [Fact] void should_reject_an_expression_with_trailing_text() => _acceptsTrailingText.ShouldBeFalse();
    [Fact] void should_reject_another_expression() => _acceptsAnotherExpression.ShouldBeFalse();
}
