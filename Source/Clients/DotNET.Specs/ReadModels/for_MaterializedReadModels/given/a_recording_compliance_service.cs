// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CA2263

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModels.given;

/// <summary>
/// The materialized read model surface with a compliance service that records every release request and
/// answers it the way the kernel does for a value encrypted under the requested subject.
/// </summary>
public class a_recording_compliance_service : for_ReadModels.given.all_dependencies
{
    protected ICompliance _compliance;
    protected List<ReleaseRequest> _requests;

    /// <summary>
    /// Produce the ciphertext the kernel would have written for a value under a subject.
    /// </summary>
    /// <param name="subject">The subject the value was encrypted under.</param>
    /// <param name="value">The plaintext value.</param>
    /// <returns>The ciphertext.</returns>
    protected static string Cipher(string subject, string value) => $"cipher({subject}):{value}";

    /// <summary>
    /// Make the materialized read model surface return one stored instance.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model the surface serves.</typeparam>
    /// <param name="json">The stored instance as JSON.</param>
    protected void StoredInstance<TReadModel>(string json)
    {
        _projections.HasFor(typeof(TReadModel)).Returns(true);
        var materialized = Substitute.For<Contracts.ReadModels.IMaterializedReadModels>();
        _services.MaterializedReadModels.Returns(materialized);
        materialized.GetInstances(Arg.Any<GetInstancesRequest>()).Returns(new GetInstancesResponse
        {
            Instances = [json],
            TotalCount = 1
        });
    }

    void Establish()
    {
        var schema = new JsonSchema();
        schema.ExtensionData[ComplianceJsonSchemaExtensions.ComplianceKey] = new List<ComplianceSchemaMetadata> { new("PII", "{}") };
        _schemaGenerator.Generate(Arg.Any<Type>()).Returns(schema);

        _requests = [];
        _compliance = Substitute.For<ICompliance>();
        _services.Compliance.Returns(_compliance);
        _compliance.Release(Arg.Any<ReleaseRequest>()).Returns(call =>
        {
            var request = call.Arg<ReleaseRequest>();
            _requests.Add(request);
            return Task.FromResult(new ReleaseResponse { Payload = Decrypt(request.Subject, request.Payload) });
        });
    }

    static string Decrypt(string subject, string payload)
    {
        var json = JsonNode.Parse(payload)!.AsObject();
        foreach (var (key, value) in json.ToArray())
        {
            if (value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var text) && text.StartsWith("cipher(", StringComparison.Ordinal))
            {
                json[key] = text.StartsWith(Cipher(subject, string.Empty), StringComparison.Ordinal)
                    ? text[Cipher(subject, string.Empty).Length..]
                    : string.Empty;
            }
        }

        return json.ToJsonString();
    }
}
#pragma warning restore CA2263
