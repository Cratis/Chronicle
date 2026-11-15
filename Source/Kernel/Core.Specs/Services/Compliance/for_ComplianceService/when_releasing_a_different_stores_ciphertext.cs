// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService;

public class when_releasing_a_different_stores_ciphertext : given.a_compliance_service
{
    string _ciphertext;

    async Task Establish()
    {
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(storage, encryption);
        var otherManager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, storage, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        var schema = await JsonSchema.FromJsonAsync(request.Schema);
        var protectedContent = await otherManager.Apply("other-event-store", event_store_namespace, schema, identifier, new JsonObject { ["name"] = "other-store-private-name" });
        _ciphertext = protectedContent.ToJsonString();
        request.Payload = _ciphertext;
        request.Strict = true;
    }

    async Task Because() => result = await service.Release(request);

    [Fact] void should_not_release_under_the_addressed_stores_keys() => result.HasError.ShouldBeTrue();
    [Fact] void should_not_return_the_other_stores_plaintext() => result.Payload.ShouldNotContain("other-store-private-name");
    [Fact] void should_have_used_real_ciphertext() => _ciphertext.ShouldNotContain("other-store-private-name");
}
