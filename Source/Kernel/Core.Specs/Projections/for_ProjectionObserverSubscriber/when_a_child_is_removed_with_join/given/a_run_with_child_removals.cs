// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_a_child_is_removed_with_join.given;

public class a_run_with_child_removals : for_ProjectionObserverSubscriber.given.a_pinned_subscriber
{
    protected ExpandoObject _state;
    protected IChangeset<AppendedEvent, ExpandoObject> _changeset;

    protected virtual IEnumerable<Change> ChangesOfTheRun => [new ChildRemovedFromAll(new PropertyPath("groups"), new PropertyPath("groupId"), Guid.NewGuid(), ArrayIndexers.NoIndexers)];

    void Establish()
    {
        _event = _event with { Context = _event.Context with { EventType = _pin } };
        _state = new ExpandoObject();
        var values = (IDictionary<string, object?>)_state;
        values[WellKnownProperties.ReadModelInstanceInitialized] = true;
        values["fullName"] = "Ada Lovelace";

        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(new JsonObject());

        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.HasChanges.Returns(true);
        _changeset.CurrentState.Returns(_state);
        _changeset.Changes.Returns(ChangesOfTheRun);
        _cachedPipeline.Handle(_event).Returns(new ProjectionEventContext("the-removed-group", _event, _changeset, ProjectionOperationType.Join | ProjectionOperationType.Remove, false));
    }
}
