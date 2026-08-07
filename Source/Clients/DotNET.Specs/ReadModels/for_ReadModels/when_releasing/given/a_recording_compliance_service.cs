// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_releasing.given;

/// <summary>
/// A compliance service that records every release request and answers it the way the kernel does for a
/// value encrypted under the requested subject: the ciphertext <c>cipher(subject):value</c> comes back as
/// <c>value</c>, and a ciphertext belonging to another subject comes back blank, which is what the kernel
/// degrades an undecryptable property to.
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
    /// Get the release request issued for a subject.
    /// </summary>
    /// <param name="subject">The subject to look for.</param>
    /// <returns>The <see cref="ReleaseRequest"/>.</returns>
    protected ReleaseRequest RequestFor(string subject) => _requests.Single(request => request.Subject == subject);

    /// <summary>
    /// Get the property names a release request carried.
    /// </summary>
    /// <param name="subject">The subject the request was issued for.</param>
    /// <returns>The property names in the request payload.</returns>
    protected IEnumerable<string> PayloadKeysFor(string subject) =>
        JsonNode.Parse(RequestFor(subject).Payload)!.AsObject().Select(entry => entry.Key);

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
