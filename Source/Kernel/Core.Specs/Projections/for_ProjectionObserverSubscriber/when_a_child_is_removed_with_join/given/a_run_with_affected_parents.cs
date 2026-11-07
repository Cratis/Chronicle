// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_a_child_is_removed_with_join.given;

public class a_run_with_affected_parents : a_run_with_child_removals
{
    protected Key _firstParent;
    protected Key _secondParent;
    protected ExpandoObject _firstParentState;
    protected ExpandoObject _secondParentState;
    protected IReadOnlyList<(Key Key, ExpandoObject State)> _currentParents;

    void Establish()
    {
        _firstParent = new Key("user-1", ArrayIndexers.NoIndexers);
        _secondParent = new Key("user-2", ArrayIndexers.NoIndexers);
        _firstParentState = StateOf("Ada Lovelace");
        _secondParentState = StateOf("Grace Hopper");
        _currentParents = [(_firstParent, _firstParentState), (_secondParent, _secondParentState)];

        var context = new ProjectionEventContext("the-removed-group", _event, _changeset, ProjectionOperationType.Join | ProjectionOperationType.Remove, false);
        context.AddAffectedParentKeys([_firstParent, _secondParent]);
        _cachedPipeline.Handle(_event).Returns(context);
        _cachedPipeline.ReadCurrent(Arg.Any<IReadOnlyCollection<Key>>()).Returns(_ => Task.FromResult(CurrentParents()));
    }

    protected virtual IReadOnlyList<(Key Key, ExpandoObject State)> CurrentParents() => _currentParents;

    static ExpandoObject StateOf(string name)
    {
        var state = new ExpandoObject();
        var values = (IDictionary<string, object?>)state;
        values[WellKnownProperties.ReadModelInstanceInitialized] = true;
        values["fullName"] = name;
        return state;
    }
}
