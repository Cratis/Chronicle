// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors.when_registering;

public class and_shutdown_drains_an_admitted_write : given.registrations
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _disposedBeforeRelease;
    Exception? _activeFailure;
    Exception? _queuedFailure;

    void Establish() => _definitions.Save(Arg.Any<ReactorDefinition>()).Returns(async call =>
    {
        _entered.SetResult();
        await _release.Task;
        var definition = call.Arg<ReactorDefinition>();
        _persisted[definition.Identifier] = definition;
    });

    async Task Because()
    {
        var active = _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default);
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var queued = _reactors.DiscoverAndRegister(_eventStore, "queued");
        var disposing = _reactors.DisposeAsync().AsTask();
        _disposedBeforeRelease = disposing.IsCompleted;
        try
        {
            _queuedFailure = await Catch.Exception(() => queued.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        finally
        {
            _release.SetResult();
        }

        _activeFailure = await Catch.Exception(() => active.WaitAsync(TimeSpan.FromSeconds(5)));
        await disposing.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_wait_for_the_admitted_write_before_disposing() => _disposedBeforeRelease.ShouldBeFalse();
    [Fact] void should_cancel_queued_admission() => _queuedFailure.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_not_mask_the_active_result_with_disposed_semaphore_errors() => _activeFailure.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] async Task should_persist_only_the_admitted_definition() => await _definitions.Received(1).Save(Arg.Any<ReactorDefinition>());
}
