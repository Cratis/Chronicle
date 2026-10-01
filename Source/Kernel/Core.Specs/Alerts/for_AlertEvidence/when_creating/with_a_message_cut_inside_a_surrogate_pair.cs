// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_AlertEvidence.when_creating;

/// <summary>
/// A message that would be cut in the middle of a surrogate pair is cut before it, so the result is always valid text.
/// </summary>
public class with_a_message_cut_inside_a_surrogate_pair : Specification
{
    AlertEvidence _result;

    void Because() => _result = AlertEvidence.Create(1, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, FailureKind.Handling, new string('a', 199) + "\U0001F600tail");

    [Fact] void should_cut_before_the_pair() => _result.Message.ShouldEqual(new string('a', 199));
}
