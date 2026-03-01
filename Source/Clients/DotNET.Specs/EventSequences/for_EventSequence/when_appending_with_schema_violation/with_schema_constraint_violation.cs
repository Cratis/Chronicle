// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_schema_violation;

public class with_schema_constraint_violation : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    string _event;
    EventType _eventType;
    JsonObject _eventContext;
    IEnumerable<Causation> _causation;
    Identity _causedBy;
    AppendResponse _response;
    IAppendResult _result;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _event = "Actual event";
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);

        _eventContext = [];
        _eventSerializer.Serialize(_event).Returns(_eventContext);

        _causation =
        [
            new Causation(DateTimeOffset.UtcNow, Guid.NewGuid().ToString(), new Dictionary<string, string> { { "key", "42" } })
        ];

        _causedBy = new("Subject", "Name", "UserName", new("BehalfOf_Subject", "BehalfOf_Name", "BehalfOf_UserName"));

        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(_eventType);
        _causationManager.GetCurrentChain().Returns(_causation.ToImmutableList());
        _identityProvider.GetCurrent().Returns(_causedBy);

        _response = new()
        {
            CorrelationId = Guid.NewGuid(),
            SequenceNumber = 0,
            ConstraintViolations =
            [
                new()
                {
                    EventTypeId = _eventType.Id.Value,
                    SequenceNumber = 0,
                    ConstraintType = Contracts.Events.Constraints.ConstraintType.Schema,
                    ConstraintName = "SchemaValidation",
                    Message = "PropertyRequired: #/name",
                    Details = new Dictionary<string, string>
                    {
                        ["path"] = "#/name",
                        ["kind"] = "PropertyRequired"
                    }
                }
            ],
            Errors = []
        };

        _serviceAccessor.Services.EventSequences.Append(Arg.Any<AppendRequest>(), CallContext.Default).Returns(_response);
    }

    async Task Because() => _result = await _eventSequence.Append(_eventSourceId, _event);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_constraint_violations() => _result.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_have_one_violation() => _result.ConstraintViolations.Count().ShouldEqual(1);
    [Fact] void should_have_schema_constraint_type() => _result.ConstraintViolations.First().ConstraintType.ShouldEqual(ConstraintType.Schema);
    [Fact] void should_have_schema_validation_constraint_name() => _result.ConstraintViolations.First().ConstraintName.Value.ShouldEqual("SchemaValidation");
    [Fact] void should_have_violation_message() => _result.ConstraintViolations.First().Message.Value.ShouldEqual("PropertyRequired: #/name");
    [Fact] void should_have_violation_details_with_path() => _result.ConstraintViolations.First().Details["path"].ShouldEqual("#/name");
    [Fact] void should_have_violation_details_with_kind() => _result.ConstraintViolations.First().Details["kind"].ShouldEqual("PropertyRequired");
}
