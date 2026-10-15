// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors.when_registering;

public class and_a_queued_registration_is_cancelled : given.registrations
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception? _failure;

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
        using var cancellation = new CancellationTokenSource();
        var queued = _reactors.DiscoverAndRegister(_eventStore, "queued", cancellation.Token);
        await cancellation.CancelAsync();
        try
        {
            _failure = await Catch.Exception(() => queued.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        finally
        {
            _release.SetResult();
        }

        await active.WaitAsync(TimeSpan.FromSeconds(5));
        await _reactors.DiscoverAndRegister(_eventStore, "after-cancellation");
    }

    [Fact] void should_cancel_the_queued_registration() => _failure.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] async Task should_not_write_cancelled_or_unchanged_metadata() => await _definitions.Received(1).Save(Arg.Any<ReactorDefinition>());
}
