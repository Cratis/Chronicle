// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Serialization;

namespace Cratis.Chronicle.Schemas.for_CompensationSchemaProcessor.given;

public class a_json_schema_generator_for<T> : Specification
{
    protected JsonSchema _schema;
    protected JsonSchemaGenerator _generator;

    void Establish()
    {
        _generator = new(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()),
            new DefaultNamingPolicy());

        _schema = _generator.Generate(typeof(T));
    }
}
