// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_DecryptInitialState.when_performing;

public class and_initial_state_has_fencing_metadata : given.all_dependencies
{
    ProjectionEventContext _context;
    IDictionary<string, object?> _lastHandled;

    void Establish()
    {
        _schema.Properties["name"] = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata("PII", string.Empty) } }
            }
        };

        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(new ExpandoObject());
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(_ => new JsonObject());

        _lastHandled = new ExpandoObject();
        _lastHandled["a%2Eb"] = 42L;
        dynamic state = new ExpandoObject();
        state.name = "encrypted-name";
        var stored = (IDictionary<string, object?>)state;
        stored[WellKnownProperties.Subject] = "stored-subject";
        stored[WellKnownProperties.Revision] = 7L;
        stored[WellKnownProperties.Incarnation] = "incarnation-1";
        stored[WellKnownProperties.LastHandledByNamespace] = _lastHandled;
        _context = CreateContext(state);
    }

    async Task Because() => await _step.Perform(_projection, _context);

    [Fact] void should_keep_the_revision() => State[WellKnownProperties.Revision].ShouldEqual(7L);
    [Fact] void should_keep_the_incarnation() => State[WellKnownProperties.Incarnation].ShouldEqual("incarnation-1");
    [Fact] void should_keep_the_last_handled_per_namespace() => State[WellKnownProperties.LastHandledByNamespace].ShouldEqual(_lastHandled);

    IDictionary<string, object?> State => _context.Changeset.InitialState;
}
