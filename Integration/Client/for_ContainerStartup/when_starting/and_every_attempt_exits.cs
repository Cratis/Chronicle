// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ContainerStartup.when_starting;

public class and_every_attempt_exits : given.a_container_startup
{
    Exception _lastFailure;

    void Establish()
    {
        foreach (var container in _containers)
        {
            _lastFailure = Crash(container);
            Fail(container, _lastFailure);
        }
        _inspect = (container, _) => Task.FromResult<ContainerExit?>(new(Array.IndexOf(_containers, container) + 1, false, ""));
        _readTail = (container, _) => Task.FromResult($"tail:{container.Id}");
    }

    async Task Because() => _failure = await Catch.Exception(Start);

    [Fact] void should_throw_the_bounded_failure() => _failure.ShouldBeOfExactType<ContainerStartupFailed>();
    [Fact] void should_keep_three_attempts() => ((ContainerStartupFailed)_failure).Attempts.Count.ShouldEqual(3);
    [Fact] void should_include_each_exit_code() => Enumerable.Range(1, 3).All(code => _failure.Message.Contains($"exit code {code}", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_include_each_tail() => _containers.All(container => _failure.Message.Contains($"tail:{container.Id}", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_dispose_every_container() => _calls.Where(call => call.StartsWith("dispose:", StringComparison.Ordinal)).ShouldEqual(["dispose:container-1", "dispose:container-2", "dispose:container-3"]);
    [Fact] void should_preserve_the_last_failure() => _failure.InnerException.ShouldEqual(_lastFailure);
}
