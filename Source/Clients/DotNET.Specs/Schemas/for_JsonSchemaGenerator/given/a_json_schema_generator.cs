// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Serialization;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.given;

public class a_json_schema_generator : Specification
{
    protected TypeFormats _typeFormats;
    protected JsonSchemaGenerator _generator;
    protected INamingPolicy _namingPolicy = new DefaultNamingPolicy();

    void Establish()
    {
        _typeFormats = new();

        _generator = new(new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()),
            _namingPolicy);
    }
}
