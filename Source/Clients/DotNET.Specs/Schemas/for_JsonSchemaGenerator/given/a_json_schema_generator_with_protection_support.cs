// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Confidentiality;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.given;

public class a_json_schema_generator_with_protection_support : a_json_schema_generator
{
    void Establish()
    {
        var piiProvider = new PIIMetadataProvider();
        var encryptedProvider = new EncryptedMetadataProvider();
        _generator = new(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>([piiProvider]),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>([piiProvider])),
            new SecurityMetadataResolver(
                new KnownInstancesOf<ICanProvideSecurityMetadataForType>([encryptedProvider]),
                new KnownInstancesOf<ICanProvideSecurityMetadataForProperty>([encryptedProvider])),
            _namingPolicy);
    }
}
