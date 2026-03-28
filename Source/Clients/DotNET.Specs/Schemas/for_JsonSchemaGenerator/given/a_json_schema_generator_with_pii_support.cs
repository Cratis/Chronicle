// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Serialization;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.given;

public class a_json_schema_generator_with_pii_support : Specification
{
    protected TypeFormats _typeFormats;
    protected JsonSchemaGenerator _generator;
    protected INamingPolicy _namingPolicy = new CamelCaseNamingPolicy();

    void Establish()
    {
        _typeFormats = new();

        var piiProvider = new PIIMetadataProvider();
        _generator = new(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>([piiProvider]),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>([piiProvider])),
            _namingPolicy);
    }
}
