// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

/// <summary>
/// A disposal that runs while the server stream is still being created, which is the only window in which the
/// watcher does not yet hold the stream, must not leave that stream open once creation completes.
/// </summary>
public class when_disposing_while_the_stream_is_being_opened : given.a_watcher
{
    readonly Subject<ReadModelChangeset> _serverStream = new();
    readonly ManualResetEventSlim _creating = new();
    readonly ManualResetEventSlim _disposed = new();
    Task _starting;

    void Establish() =>
        _readModelsService.Watch(Arg.Any<WatchRequest>()).Returns(_ =>
        {
            _creating.Set();
            _disposed.Wait(TimeSpan.FromSeconds(30));
            return _serverStream;
        });

    async Task Because()
    {
        _starting = Task.Run(_watcher.Start);
        _creating.Wait(TimeSpan.FromSeconds(30));
        _watcher.Dispose();
        _disposed.Set();
        await _starting;
    }

    [Fact] void should_not_leave_the_stream_open() => _serverStream.HasObservers.ShouldBeFalse();
}
