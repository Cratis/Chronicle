// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_an_event_flows_through_one_of_two_namespaces : given.a_factory_for_two_namespaces
{
    IProjection _firstProjection;
    IProjection _secondProjection;
    Changeset<AppendedEvent, ExpandoObject> _firstChangeset;
    Changeset<AppendedEvent, ExpandoObject> _secondChangesetAfterDisposingFirst;
    int _secondStorageReadsAfterFirstEvent;

    async Task Because()
    {
        _firstProjection = await _factory.Create(_eventStore, _first, _definition, _readModel, []);
        _secondProjection = await _factory.Create(_eventStore, _second, _definition, _readModel, []);
        _firstChangeset = Project(_firstProjection);
        _secondStorageReadsAfterFirstEvent = _secondStorage.ReceivedCalls().Count();
        ((IDictionary<string, object?>)_firstProjection.InitialModelState)["mutated"] = true;
        ((IDisposable)_firstProjection).Dispose();
        _secondChangesetAfterDisposingFirst = Project(_secondProjection);
    }

    [Fact] void should_project_into_the_namespace_it_flowed_through() => NameOf(_firstChangeset).ShouldEqual("alpha");
    [Fact] void should_not_touch_the_other_namespaces_storage() => _secondStorageReadsAfterFirstEvent.ShouldEqual(0);
    [Fact] void should_not_leak_initial_state_changes_to_the_other_namespace() => ((IDictionary<string, object?>)_secondProjection.InitialModelState).ContainsKey("mutated").ShouldBeFalse();
    [Fact] void should_keep_projecting_in_the_other_namespace_after_one_is_disposed() => NameOf(_secondChangesetAfterDisposingFirst).ShouldEqual("beta");
}
