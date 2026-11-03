// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class and_the_reducer_has_no_dispatchable_methods : given.a_reducer_backed_model
{
    void Establish() => _handler.EventTypes.Returns([]);

    void Because()
    {
        Watch();
        SendLocalChanges();
    }

    [Fact] void should_use_only_the_local_watcher() => _readModelWatcherManager.DidNotReceive().GetWatcher<Model>();
    [Fact] void should_keep_the_payload() => _changes[0].ReadModel.ShouldEqual(_model);
    [Fact] void should_report_modified_then_removed() => _changes.Select(_ => _.ChangeType).ShouldEqual([ReadModelChangeType.Modified, ReadModelChangeType.Removed]);
}
