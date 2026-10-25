// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyEventSourceIdKeys.when_normalizing;

public class and_one_derivative_declares_the_property : given.a_projection_definition
{
    void Establish()
    {
        _schemas = [Schema(_type, "name"), Schema(_otherType, "EventSourceId")];
        _definition = Definition(fromDerivatives: [new FromDerivatives([_type, _otherType], From("EventSourceId", "EventSourceId"))]);
    }

    void Because() => _result = LegacyEventSourceIdKeys.Normalize(_definition, _schemas, _rewritten);

    [Fact] void should_keep_the_shared_payload_key() => _result.FromDerivatives.Single().From.Key.Value.ShouldEqual("EventSourceId");
    [Fact] void should_keep_the_shared_payload_parent_key() => _result.FromDerivatives.Single().From.ParentKey!.Value.ShouldEqual("EventSourceId");
}
