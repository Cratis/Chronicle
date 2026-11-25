// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

[PerFactSpecificationLifecycle]
public class when_flat_key_properties_are_not_supported : given.a_runtime_language_service
{
    const string Body = "from Changed\n  key\n    customer = name\n    number = age";

    async Task Establish()
    {
        SetSchema("{\"type\":\"string\"}");
        await Compile(Body);
    }

    [Theory]
    [InlineData("compliance", "PII", false)]
    [InlineData("compliance", "PII", true)]
    [InlineData("compliance", "CustomProtection", false)]
    [InlineData("compliance", "CustomProtection", true)]
    [InlineData("security", "EncryptedSubject", false)]
    [InlineData("security", "EncryptedSubject", true)]
    [InlineData("security", "EncryptedNamespace", true)]
    [InlineData("security", "EncryptedGlobal", true)]
    void should_refuse_protected_parts_in_both_pdl_and_direct_definitions(string category, string metadataType, bool referenced)
    {
        var property = $$"""{"type":"string","{{category}}":[{"metadataType":"{{metadataType}}","details":""}]}""";
        SetSchema(property, referenced);
        AssertRefused("metadata");
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"type\":\"null\"}")]
    [InlineData("{\"type\":[\"string\",\"integer\"]}")]
    [InlineData("{\"type\":\"object\"}")]
    [InlineData("{\"type\":\"array\",\"items\":{\"type\":\"string\"}}")]
    void should_use_the_same_scalar_rules_in_pdl_and_direct_definitions(string property)
    {
        SetSchema(property);
        AssertRefused("scalar");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_accept_an_unprotected_nullable_scalar(bool referenced)
    {
        SetSchema("{\"type\":[\"string\",\"null\"]}", referenced);
        await Compile(Body);
        FlatCompositeKeyDefinition.Validate(_definition, _readModel);
    }

    void AssertRefused(string message)
    {
        var exception = Catch.Exception(() => FlatCompositeKeyDefinition.Validate(_definition, _readModel));
        exception.ShouldBeOfExactType<FlatCompositeKeyNotSupported>();
        exception.Message.ShouldContain(message);
        var source = "projection Alerts => Alert\n" + string.Join('\n', Body.Split('\n').Select(line => "  " + line));
        var result = _language.Compile(source, ProjectionOwner.Client, [_readModel], _schemas);
        result.Match(_ => false, errors => errors.Errors.Any(error => error.Message.Contains(message))).ShouldBeTrue();
    }

    void SetSchema(string property, bool referenced = false) =>
        _readModel.Schemas[ReadModelGeneration.First] = JsonSchema.FromJson($$"""
            {
                "type": "object",
                "properties": {
                    "customer": {{(referenced ? "{\"$ref\":\"#/$defs/Intermediate\"}" : property)}},
                    "number": {"type": "integer"}
                },
                "$defs": {
                    "Intermediate": {"$ref": "#/$defs/Value"},
                    "Value": {{property}}
                }
            }
            """);
}
