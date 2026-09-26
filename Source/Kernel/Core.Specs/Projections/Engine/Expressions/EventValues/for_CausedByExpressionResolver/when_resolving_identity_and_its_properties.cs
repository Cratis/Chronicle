// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_CausedByExpressionResolver;

public class when_resolving_identity_and_its_properties : given.an_appended_event
{
    CausedByExpressionResolver _resolver;
    Identity _identity;
    object _wholeIdentity;
    object _userName;

    void Establish()
    {
        _resolver = new();
        _identity = @event.Context.CausedBy;
    }

    void Because()
    {
        _wholeIdentity = _resolver.Resolve(WellKnownExpressions.CausedBy)(@event);
        _userName = _resolver.Resolve($"{WellKnownExpressions.CausedBy}(userName)")(@event);
    }

    [Fact] void should_resolve_the_identity() => _wholeIdentity.ShouldEqual(_identity);
    [Fact] void should_resolve_the_identity_property() => _userName.ShouldEqual(_identity.UserName);
}
