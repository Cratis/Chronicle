// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Retrying is for a cluster that is still settling, and a cluster has settled long before the
/// budget is spent. A step still timing out after that is not waiting on anything, so it fails the
/// host exactly as it did before - starting around it would leave the silo quietly half-initialized,
/// which is the harder failure to find.
/// </summary>
public class and_a_sibling_silo_never_becomes_reachable : given.a_startup_task
{
    Exception _error;

    void Establish() =>
        _patternCapture.Subscribe(_eventStore, _namespace)
            .Returns(_ => Task.FromException(new TimeoutException("Response did not arrive on time")));

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_fail() => _error.ShouldNotBeNull();
    [Fact] void should_fail_with_the_underlying_timeout() => _error.ShouldBeOfExactType<TimeoutException>();
    [Fact] async Task should_have_exhausted_its_attempts_first() => await _patternCapture.Received(5).Subscribe(_eventStore, _namespace);
}
