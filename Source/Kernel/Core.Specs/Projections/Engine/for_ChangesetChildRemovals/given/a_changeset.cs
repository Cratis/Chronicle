// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ChangesetChildRemovals.given;

public class a_changeset : Specification
{
    protected Changeset<AppendedEvent, ExpandoObject> _changeset;

    void Establish() =>
        _changeset = new(
            Substitute.For<IObjectComparer>(),
            AppendedEvent.EmptyWithEventType(new EventType("TestEvent", EventTypeGeneration.First)),
            new ExpandoObject());

    protected void RemoveChildFromAll() =>
        _changeset.RemoveChildFromAll(new PropertyPath("groups"), new PropertyPath("groupId"), Guid.NewGuid(), ArrayIndexers.NoIndexers);
}
