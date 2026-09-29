// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_KernelConnectionErrors.when_checking_if_interrupted_by_shutdown;

public class and_the_reactor_is_stopping_with_a_genuine_failure : Specification
{
    bool _result;

    void Because() => _result = new InvalidOperationException("Something broke").IsInterruptedByShutdown(new CancellationToken(canceled: true));

    [Fact] void should_not_be_interrupted_by_shutdown() => _result.ShouldBeFalse();
}
