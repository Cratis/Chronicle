// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_AppendManyResult;

public class when_has_concurrency_violations : Specification
{
    AppendManyResult _result;
    ConcurrencyViolation _violation;

    void Establish()
    {
        _violation = new ConcurrencyViolation(42ul, 43ul);
        _result = new AppendManyResult
        {
            ConcurrencyViolations = new Dictionary<EventSourceId, ConcurrencyViolation>()
            {
                {
                    "event-source-id",
                    _violation
                }
            }
        };
    }

    [Fact] void should_have_concurrency_violations() => _result.HasConcurrencyViolations.ShouldBeTrue();
    [Fact] void should_not_be_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_return_the_violation() => _result.ConcurrencyViolations.Values.First().ShouldEqual(_violation);
}
