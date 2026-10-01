// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts.for_AlertEvidence.when_creating;

/// <summary>
/// A message of 200 characters or fewer is kept as it is.
/// </summary>
public class with_a_message_that_fits : Specification
{
    AlertEvidence _result;

    void Because() => _result = AlertEvidence.Create(1, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, FailureKind.Handling, new string('a', 200));

    [Fact] void should_keep_the_message() => _result.Message.ShouldEqual(new string('a', 200));
}
