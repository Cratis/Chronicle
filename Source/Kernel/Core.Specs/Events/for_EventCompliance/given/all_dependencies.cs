// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventCompliance.given;

public class all_dependencies : Specification
{
    protected const string SubjectValue = "subject-identifier";
    protected static readonly EventType SomeEventType = new(new EventTypeId(Guid.NewGuid().ToString()), EventTypeGeneration.First);
    protected static readonly EventType OtherEventType = new(new EventTypeId(Guid.NewGuid().ToString()), EventTypeGeneration.First);

    protected IJsonComplianceManager _complianceManager;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected EventCompliance _compliance;

    protected JsonSchema _schemaWithPii;

    void Establish()
    {
        _complianceManager = Substitute.For<IJsonComplianceManager>();
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();

        _schemaWithPii = new JsonSchema();
        _schemaWithPii.Properties["name"] = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                {
                    ComplianceJsonSchemaExtensions.ComplianceKey,
                    new[] { new ComplianceSchemaMetadata("PII", string.Empty) }
                }
            }
        };

        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(_ => new JsonObject { ["name"] = "encrypted-name" });

        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>())
            .Returns(_ =>
            {
                dynamic result = new ExpandoObject();
                result.name = "decrypted-name";
                return result;
            });

        _complianceManager.Release(
                Arg.Any<EventStoreName>(),
                Arg.Any<EventStoreNamespaceName>(),
                Arg.Any<JsonSchema>(),
                Arg.Any<string>(),
                Arg.Any<JsonObject>())
            .Returns(_ => Task.FromResult(new JsonObject { ["name"] = "decrypted-name" }));

        _compliance = new EventCompliance(_complianceManager, _expandoObjectConverter);
    }
}
