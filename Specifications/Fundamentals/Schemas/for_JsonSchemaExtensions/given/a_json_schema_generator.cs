// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;

namespace Aksio.Cratis.Schemas.for_JsonSchemaExtensions.given;

public class a_json_schema_generator : Specification
{
    protected TypeFormats type_formats;
    protected JsonSchemaGenerator generator;

    void Establish()
    {
        type_formats = new();

        generator = new(new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()));
    }
}
