// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using NSubstitute;

namespace Cratis.Chronicle.Integration.for_ContainerStartup.given;

public class a_container_startup : Cratis.Specifications.Specification
{
    protected IContainer[] _containers;
    protected List<string> _calls;
    protected List<string> _reports;
    protected List<IContainer> _built;
    protected List<ContainerStartupAttempt> _retries;
    protected Func<IContainer, CancellationToken, Task<ContainerExit?>> _inspect;
    protected Func<IContainer, CancellationToken, Task<string>> _readTail;
    protected ContainerStartup _startup;
    protected IContainer _result;
    protected Exception _failure;

    void Establish()
    {
        _calls = [];
        _reports = [];
        _built = [];
        _retries = [];
        _containers = Enumerable.Range(1, 3).Select(index =>
        {
            var container = Substitute.For<IContainer>();
            container.Id.Returns($"container-{index}");
            container.Image.Returns(new DockerImage(SqlServerContainerImage.Name));
            container.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
#pragma warning disable CA2012 // Configures a substitute; no real ValueTask is being consumed.
            container.DisposeAsync().Returns(_ =>
            {
                _calls.Add($"dispose:{container.Id}");
                return ValueTask.CompletedTask;
            });
#pragma warning restore CA2012
            return container;
        }).ToArray();
        var queue = new Queue<IContainer>(_containers);
        _inspect = (_, _) => Task.FromResult<ContainerExit?>(null);
        _readTail = (_, _) => Task.FromResult("tail");
        _startup = new(
            queue.Dequeue,
            (container, token) =>
            {
                _calls.Add($"inspect:{container.Id}");
                return _inspect(container, token);
            },
            (container, token) =>
            {
                _calls.Add($"tail:{container.Id}");
                return _readTail(container, token);
            },
            _reports.Add,
            TimeSpan.FromMilliseconds(200));
    }

    protected Task Start() => Start(CancellationToken.None);

    protected async Task Start(CancellationToken token) =>
        _result = await _startup.Start(_built.Add, _retries.Add, token);

    protected void Fail(IContainer container, Exception failure) =>
        container.StartAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(failure));

    protected static ContainerNotRunningException Crash(IContainer container) =>
        new(container.Id, "", "", 1, new IOException("container is not running"));
}
