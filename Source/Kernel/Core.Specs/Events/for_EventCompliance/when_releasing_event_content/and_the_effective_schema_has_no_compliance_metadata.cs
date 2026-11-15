// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventCompliance.when_releasing_event_content;

public class and_the_effective_schema_has_no_compliance_metadata : given.all_dependencies
{
    AppendedEvent _event;
    AppendedEvent _result;

    void Establish()
    {
        dynamic content = new ExpandoObject();
        content.name = "plain-name";

        _event = new AppendedEvent(
            EventContext.Empty with { Subject = new Subject(SubjectValue) },
            content);
    }

    async Task Because() => _result = await _compliance.Release(_event, new JsonSchema());

    [Fact] void should_return_the_event_unchanged() => ReferenceEquals(_result, _event).ShouldBeTrue();
    [Fact] void should_not_convert_the_content() => _expandoObjectConverter.DidNotReceive().ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>());
    [Fact] void should_not_call_compliance_manager_release() =>
        _complianceManager.DidNotReceive().Release(
            Arg.Any<EventStoreName>(),
            Arg.Any<EventStoreNamespaceName>(),
            Arg.Any<JsonSchema>(),
            Arg.Any<string>(),
            Arg.Any<JsonObject>());
}
