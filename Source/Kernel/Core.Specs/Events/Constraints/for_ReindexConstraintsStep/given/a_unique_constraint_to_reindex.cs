// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.given;

public class a_unique_constraint_to_reindex : Specification
{
    protected const string Property = "Email";

    protected IUniqueConstraintsStorage _storage;
    protected UniqueConstraintDefinition _definition;
    protected UniqueConstraintValidator _validator;
    protected HashSet<(EventSourceId EventSourceId, string ScopeKey)> _seen;
    protected EventType _eventType;

    void Establish()
    {
        _storage = Substitute.For<IUniqueConstraintsStorage>();
        _eventType = new("SomeEvent", 1);
        _definition = new("SomeConstraint", [new(_eventType.Id, [Property])]);
        _validator = new(_definition, _storage);
        _seen = [];
    }

    protected static ExpandoObject ContentWith(string value)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)[Property] = value;
        return content;
    }

    protected AppendedEvent EventFor(EventSourceId eventSourceId) =>
        new(
            EventContext.From(
                EventStoreName.NotSet,
                EventStoreNamespaceName.NotSet,
                _eventType,
                EventSourceType.Default,
                eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                EventSequenceNumber.First,
                CorrelationId.NotSet),
            new ExpandoObject());
}
