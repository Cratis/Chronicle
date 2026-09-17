// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// A silo that answers late while the cluster is still forming is waited out, not died on. This is
/// the whole point: an unhandled timeout here terminates the host, both replicas restart onto new
/// addresses, and each then cannot reach the other either - a crash loop that repairs nothing and
/// takes every consuming application's read side down with it.
/// </summary>
public class and_a_sibling_silo_is_not_reachable_yet : given.a_startup_task
{
    Exception _error;

    void Establish() =>
        _patternCapture.Subscribe(_eventStore, _namespace)
            .Returns(
                _ => Task.FromException(new TimeoutException("Response did not arrive on time")),
                _ => Task.CompletedTask);

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_not_fail_the_host() => _error.ShouldBeNull();
    [Fact] async Task should_have_tried_the_step_again() => await _patternCapture.Received(2).Subscribe(_eventStore, _namespace);
}
