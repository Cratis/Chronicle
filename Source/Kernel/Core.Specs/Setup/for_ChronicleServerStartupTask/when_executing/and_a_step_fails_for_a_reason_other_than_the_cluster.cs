// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Only membership instability is worth another attempt. A defect in the work itself fails the same
/// way every time, so retrying it would do nothing but delay a real error behind minutes of backoff.
/// </summary>
public class and_a_step_fails_for_a_reason_other_than_the_cluster : given.a_startup_task
{
    Exception _error;

    void Establish() =>
        _patternCapture.Subscribe(_eventStore, _namespace)
            .Returns(_ => Task.FromException(new InvalidOperationException("something is genuinely wrong")));

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_fail() => _error.ShouldNotBeNull();
    [Fact] async Task should_not_have_tried_again() => await _patternCapture.Received(1).Subscribe(_eventStore, _namespace);
}
