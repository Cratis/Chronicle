// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBContainerStartExtensions;

public class when_the_container_never_becomes_ready : Specification
{
    IContainer _container;
    Exception _error;

    void Establish()
    {
        _container = Substitute.For<IContainer>();

        // Testcontainers reports a wait strategy cancelled by the start token as a TimeoutException.
        _container.StartAsync(Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var token = call.Arg<CancellationToken>();
            var cancelled = new TaskCompletionSource();
            await using var registration = token.Register(cancelled.SetResult);
            await cancelled.Task;
            throw new TimeoutException("The operation has timed out.");
        });
        _container.GetLogsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(("never-ready", "mongod did not start")));
    }

    async Task Because() => _error = await Catch.Exception(() => _container.StartMongoDBWithDiagnostics(TimeSpan.FromMilliseconds(200)));

    [Fact] void should_fail_with_a_start_timeout() => _error.ShouldBeOfExactType<MongoDBContainerStartTimedOut>();
    [Fact] void should_include_the_container_stdout() => _error.Message.ShouldContain("never-ready");
    [Fact] void should_include_the_container_stderr() => _error.Message.ShouldContain("mongod did not start");
}
