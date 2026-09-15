// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_previewing;

/// <summary>
/// Verifies that PII fields are decrypted before a previewed read model instance reaches the client - the gap
/// reported in #3983, where the Projection editor's live preview showed ciphertext because Preview projected
/// straight off the (encrypted) stored events without ever calling into compliance release.
/// </summary>
public class and_the_read_model_has_a_pii_property : given.all_dependencies
{
    const string EncryptedValue = "cipher-text";
    const string DecryptedValue = "decrypted-value";
    const string Subject = "some-subject";

    OneOf<ProjectionPreview, ProjectionDeclarationParsingErrors> _result;

    void Establish()
    {
        var piiProperty = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata("PII", string.Empty) } }
            }
        };

        _readModelDefinition = _readModelDefinition with
        {
            Schemas = new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, new JsonSchema { Type = JsonObjectType.Object, Properties = { ["name"] = piiProperty } } }
            }
        };
        SetReadModels(_readModelDefinition);
        SetCompiledDefinition(_projectionDefinition);

        var projected = new ExpandoObject();
        var projectedProperties = (IDictionary<string, object?>)projected;
        projectedProperties[WellKnownProperties.Subject] = Subject;
        projectedProperties["name"] = EncryptedValue;
        SetProjectedInstances(projected);

        // Simulates the compliance manager's decryption: every property comes through unchanged except the
        // PII one, which is what proves the *released* instance - not the raw projected one - is what the
        // preview response is built from.
        _readModelsCompliance.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<ExpandoObject>())
            .Returns(callInfo =>
            {
                var released = new ExpandoObject();
                var releasedProperties = (IDictionary<string, object?>)released;
                foreach (var (key, value) in (IDictionary<string, object?>)callInfo.Arg<ExpandoObject>())
                {
                    releasedProperties[key] = value;
                }

                releasedProperties["name"] = DecryptedValue;
                return Task.FromResult(released);
            });
    }

    async Task Because() => _result = await _service.Preview(new PreviewProjectionRequest
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespace,
        EventSequenceId = "event-log",
        Declaration = "from SomeEvent set Name = Name => TestReadModel"
    });

    [Fact] void should_succeed() => _result.Value0.ShouldNotBeNull();
    [Fact] void should_return_one_entry() => _result.Value0.ReadModelEntries.Count().ShouldEqual(1);
    [Fact] void should_return_the_decrypted_value() => _result.Value0.ReadModelEntries.Single().ShouldContain(DecryptedValue);
    [Fact] void should_not_return_the_encrypted_value() => _result.Value0.ReadModelEntries.Single().Contains(EncryptedValue, StringComparison.Ordinal).ShouldBeFalse();

    [Fact]
    void should_release_compliance_for_the_projected_instance() =>
        _readModelsCompliance.Received(1).Release(
            EventStore,
            EventStoreNamespace,
            Arg.Any<JsonSchema>(),
            Arg.Is<ExpandoObject>(instance => ((IDictionary<string, object?>)instance)[WellKnownProperties.Subject].Equals(Subject)));
}
