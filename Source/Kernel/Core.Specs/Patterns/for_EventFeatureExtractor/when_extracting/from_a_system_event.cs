// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_EventFeatureExtractor.when_extracting;

public class from_a_system_event : given.an_extractor
{
    EventFeatures _result;

    void Because() => _result = _extractor.Extract(AnEvent(causedBy: Identity.System));

    [Fact] void should_recognize_the_system() => _result.InitiatorType.ShouldEqual(InitiatorType.System);
    [Fact] void should_group_by_the_system_identity() => _result.GroupingKey.Value.ShouldEqual(Identity.System.Subject);
}
