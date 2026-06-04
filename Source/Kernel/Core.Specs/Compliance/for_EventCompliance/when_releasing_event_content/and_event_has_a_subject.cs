// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Compliance.for_EventCompliance.when_releasing_event_content;

public class and_event_has_a_subject : given.all_dependencies
{
    AppendedEvent _event;
    AppendedEvent _result;

    void Establish()
    {
        dynamic content = new ExpandoObject();
        content.name = "original-name";

        _event = new AppendedEvent(
            EventContext.Empty with { Subject = new Subject(SubjectValue) },
            content);
    }

    async Task Because() => _result = await _compliance.ReleaseEventContent(_event, _schemaWithPii);

    [Fact] void should_call_compliance_manager_release() =>
        _complianceManager.Received(1).Release(
            _event.Context.EventStore,
            _event.Context.Namespace,
            _schemaWithPii,
            SubjectValue,
            Arg.Any<JsonObject>());

    [Fact] void should_return_event_with_decrypted_content() =>
        ((IDictionary<string, object?>)_result.Content)["name"].ShouldEqual("decrypted-name");

    [Fact] void should_preserve_event_context() => _result.Context.ShouldEqual(_event.Context);
}
