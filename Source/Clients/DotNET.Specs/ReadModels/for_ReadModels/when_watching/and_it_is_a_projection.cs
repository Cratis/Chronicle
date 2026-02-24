// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class and_it_is_a_projection : given.all_dependencies
{
    Subject<ReadModelChangeset<SomeModel>> _subject;
    IReadModelWatcher<SomeModel> _watcher;
    ISubject<ReadModelChangeset<SomeModel>> _result;

    void Establish()
    {
        _projections.HasFor<SomeModel>().Returns(true);
        _subject = new Subject<ReadModelChangeset<SomeModel>>();
        _watcher = Substitute.For<IReadModelWatcher<SomeModel>>();
        _watcher.Observable.Returns(_subject);
        _readModelWatcherManager.GetWatcher<SomeModel>().Returns(_watcher);
    }

    void Because() => _result = _readModels.Watch<SomeModel>();

    [Fact] void should_return_the_watcher_observable() => _result.ShouldEqual(_subject);
}
