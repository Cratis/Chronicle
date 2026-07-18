// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Json;
using Cratis.Serialization;

namespace Cratis.Chronicle.Schemas.for_OptionalReadModelValues.given;

/// <summary>
/// Sets up the schema generator and the schema-driven <see cref="ExpandoObjectConverter"/> that the Kernel uses
/// to release a projected read model back to a caller — the exact materialization path where an unset optional
/// property would otherwise be filled with a type-default sentinel.
/// </summary>
public class a_schema_driven_read_model_release : Specification
{
    protected TypeFormats _typeFormats;
    protected JsonSchemaGenerator _generator;
    protected ExpandoObjectConverter _converter;

    void Establish()
    {
        _typeFormats = new();
        _generator = new(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()),
            new DefaultNamingPolicy());
        _converter = new(_typeFormats);
    }
}
