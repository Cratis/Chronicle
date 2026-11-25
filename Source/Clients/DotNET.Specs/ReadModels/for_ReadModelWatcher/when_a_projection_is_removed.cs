// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_a_projection_is_removed : given.a_watcher
{
    readonly Subject<Contracts.ReadModels.ReadModelChangeset> _frames = new();
    ReadModelChangeset<SomeModel> _change;
    Contracts.ReadModels.WatchRequest _request;

    void Establish()
    {
        _readModelsService.Watch(Arg.Do<Contracts.ReadModels.WatchRequest>(request => _request = request)).Returns(_frames);
        _watcher.Observable.Subscribe(change => _change = change);
        _watcher.Start();
    }

    void Because() => _frames.OnNext(new()
    {
        Namespace = _eventStoreNamespace,
        ModelKey = "key",
        ReadModel = "{}",
        Removed = true,
        ChangeType = Contracts.ReadModels.ReadModelChangeType.Removed
    });

    void Destroy()
    {
        _watcher.Dispose();
        _frames.Dispose();
    }

    [Fact] void should_keep_the_projection_payload() => _change.ReadModel.ShouldNotBeNull();
    [Fact] void should_keep_the_log_sequence() => _request.EventSequenceId.ShouldEqual(EventSequenceId.Log.Value);
}
