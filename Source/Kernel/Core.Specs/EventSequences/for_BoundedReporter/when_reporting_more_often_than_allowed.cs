// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_BoundedReporter;

public class when_reporting_more_often_than_allowed : Specification
{
    BoundedReporter _reporter;
    int _allowed;

    void Establish() => _reporter = new BoundedReporter(3);

    void Because()
    {
        Parallel.For(0, 100_000, _ =>
        {
            if (_reporter.TryReport())
            {
                Interlocked.Increment(ref _allowed);
            }
        });
    }

    [Fact] void should_allow_only_the_maximum() => _allowed.ShouldEqual(3);
}
