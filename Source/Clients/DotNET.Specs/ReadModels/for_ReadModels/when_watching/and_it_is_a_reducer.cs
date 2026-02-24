// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class and_it_is_a_reducer : given.all_dependencies
{
    Subject<ReducerChangeset<SomeModel>> _reducerSubject;
    IReducerWatcher<SomeModel> _watcher;
    ISubject<ReadModelChangeset<SomeModel>> _result;

    void Establish()
    {
        _reducers.HasFor<SomeModel>().Returns(true);
        _reducerSubject = new Subject<ReducerChangeset<SomeModel>>();
        _watcher = Substitute.For<IReducerWatcher<SomeModel>>();
        _watcher.Observable.Returns(_reducerSubject);
        _reducerObservers.GetWatcher<SomeModel>().Returns(_watcher);
    }

    void Because() => _result = _readModels.Watch<SomeModel>();

    [Fact] void should_return_a_subject() => _result.ShouldNotBeNull();
}
