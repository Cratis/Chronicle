// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Compliance.for_ReadModelComplianceHelper.given;

public class all_dependencies : Specification
{
    protected static readonly EventStoreName EventStore = "test-store";
    protected static readonly EventStoreNamespaceName EventStoreNamespace = "test-namespace";
    protected const string Identifier = "subject-identifier";

    protected IJsonComplianceManager _complianceManager;
    protected IExpandoObjectConverter _converter;
    protected JsonSchema _schemaWithPii;
    protected JsonSchema _schemaWithoutPii;
    protected ExpandoObject _instance;

    void Establish()
    {
        _complianceManager = Substitute.For<IJsonComplianceManager>();
        _converter = Substitute.For<IExpandoObjectConverter>();

        _schemaWithPii = new JsonSchema();
        var property = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                {
                    ComplianceJsonSchemaExtensions.ComplianceKey,
                    new[] { new ComplianceSchemaMetadata(Guid.NewGuid(), string.Empty) }
                }
            }
        };
        _schemaWithPii.Properties["name"] = property;

        _schemaWithoutPii = new JsonSchema();
        _schemaWithoutPii.Properties["value"] = new JsonSchemaProperty();

        dynamic expando = new ExpandoObject();
        expando.name = "test-name";
        _instance = expando;

        _converter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(_ => new JsonObject { ["name"] = "test-name" });

        _converter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>())
            .Returns(_ =>
            {
                dynamic result = new ExpandoObject();
                result.name = "encrypted-name";
                return result;
            });

        _complianceManager.Apply(
                Arg.Any<EventStoreName>(),
                Arg.Any<EventStoreNamespaceName>(),
                Arg.Any<JsonSchema>(),
                Arg.Any<string>(),
                Arg.Any<JsonObject>())
            .Returns(_ => Task.FromResult(new JsonObject { ["name"] = "encrypted-name" }));

        _complianceManager.Release(
                Arg.Any<EventStoreName>(),
                Arg.Any<EventStoreNamespaceName>(),
                Arg.Any<JsonSchema>(),
                Arg.Any<string>(),
                Arg.Any<JsonObject>())
            .Returns(_ => Task.FromResult(new JsonObject { ["name"] = "decrypted-name" }));
    }
}
