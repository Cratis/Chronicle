// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts;

public class when_shortening_the_messages_of_a_failure_for_a_step : Specification
{
    IList<string> _result;

    void Because() => _result = TransientTimeouts.ShortenForStep(["The handler failed\n" + new string('x', 7000), "inner"]);

    [Fact] void should_record_one_message() => _result.Count.ShouldEqual(1);
    [Fact] void should_keep_only_the_first_line() => _result[0].ShouldEqual("The handler failed");
}
