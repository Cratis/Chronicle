// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Json;
using Cratis.Json;
using Cratis.Serialization;

namespace Cratis.Chronicle.Schemas.for_ConceptCollectionPersistence.given;

public class a_schema_driven_round_trip : Specification
{
    protected JsonSchemaGenerator _generator;
    protected ExpandoObjectConverter _converter;
    protected JsonSerializerOptions _clientOptions;

    void Establish()
    {
        _generator = new(new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()),
            new DefaultNamingPolicy());

        _converter = new(new TypeFormats());

        // Mirrors the serializer the Chronicle client uses to ship reduced/projected read-model state
        // to the Kernel — concept values flatten to their underlying primitive via these converters.
        _clientOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new EnumerableConceptAsJsonConverterFactory(),
                new ConceptAsJsonConverterFactory()
            }
        };
    }
}
