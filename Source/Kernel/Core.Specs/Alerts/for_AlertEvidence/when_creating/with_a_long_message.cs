// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_AlertEvidence.when_creating;

/// <summary>
/// A message longer than 200 characters is cut to 200, so a long exception message is not written to the event log
/// in full.
/// </summary>
public class with_a_long_message : Specification
{
    AlertEvidence _result;

    void Because() => _result = AlertEvidence.Create(1, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, FailureKind.Handling, new string('a', 201));

    [Fact] void should_keep_the_first_200_characters() => _result.Message.ShouldEqual(new string('a', 200));
}
