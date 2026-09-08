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
/// Verifies that Preview infers a compliance subject from the instance's own key when the projection never
/// stamped an explicit one - the same fallback (<c>__subject</c> -> <c>_id</c> -> <c>id</c>) already used to
/// release compliance for the Read Models views, so a previewed instance decrypts under the same rules a
/// materialized one would.
/// </summary>
public class and_the_projected_instance_has_no_explicit_subject : given.all_dependencies
{
    const string InstanceKey = "instance-42";

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

        // No __subject property - only the id the projection resolved the instance for, mirroring what a
        // projection grain actually hands back (it tags the key, not a compliance subject).
        projectedProperties["_id"] = InstanceKey;
        projectedProperties["name"] = "cipher-text";
        SetProjectedInstances(projected);

        _readModelsCompliance.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<ExpandoObject>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<ExpandoObject>()));
    }

    async Task Because() => _result = await _service.Preview(new PreviewProjectionRequest
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespace,
        EventSequenceId = "event-log",
        Declaration = "from SomeEvent set Name = Name => TestReadModel"
    });

    [Fact] void should_succeed() => _result.Value0.ShouldNotBeNull();

    [Fact]
    void should_release_compliance_with_the_instance_key_as_subject() =>
        _readModelsCompliance.Received(1).Release(
            EventStore,
            EventStoreNamespace,
            Arg.Any<JsonSchema>(),
            Arg.Is<ExpandoObject>(instance => ((IDictionary<string, object?>)instance)[WellKnownProperties.Subject].Equals(InstanceKey)));
}
