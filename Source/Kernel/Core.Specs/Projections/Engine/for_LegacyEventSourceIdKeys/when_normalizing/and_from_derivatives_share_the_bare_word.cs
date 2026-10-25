// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyEventSourceIdKeys.when_normalizing;

public class and_from_derivatives_share_the_bare_word : given.a_projection_definition
{
    void Establish() => _definition = Definition(fromDerivatives: [new FromDerivatives([_type, _otherType], From("EventSourceId", "EventSourceId"))]);

    void Because() => _result = LegacyEventSourceIdKeys.Normalize(_definition, _schemas, _rewritten);

    [Fact] void should_rewrite_the_shared_key() => _result.FromDerivatives.Single().From.Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_rewrite_the_shared_parent_key() => _result.FromDerivatives.Single().From.ParentKey!.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
    [Fact] void should_leave_the_original_key_unchanged() => _definition.FromDerivatives.Single().From.Key.Value.ShouldEqual("EventSourceId");
}
