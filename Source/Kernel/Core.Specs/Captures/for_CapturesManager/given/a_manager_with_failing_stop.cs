// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.given;

public class a_manager_with_failing_stop : a_captures_manager
{
    protected readonly TimeoutException _failure = new("Stop failed");
    protected Exception? _error;
    protected virtual bool FailureOnSave => true;

    void Establish()
    {
        StartedState(_events);
        _captures.Save(Arg.Is<Capture>(capture => capture.Status == CaptureStatus.Stopped))
            .Returns(_ => Task.FromException(_failure));
        _subscriptions.Unsubscribe(_eventStore, Arg.Any<CaptureDefinition>())
            .Returns(_ => FailureOnSave ? Task.CompletedTask : Task.FromException(_failure));
    }

    protected async Task FailStop() => _error = await Catch.Exception(() => _manager.Stop(_events.Id));

    protected async Task RecoverAutomatically()
    {
        await _manager.Ensure();
        await _manager.EnsureSubscribedIn("new-tenant");
        await _silo.TimerRegistry.FireAllAsync();
    }
}
