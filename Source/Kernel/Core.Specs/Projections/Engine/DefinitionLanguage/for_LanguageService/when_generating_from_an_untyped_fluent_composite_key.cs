// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_generating_from_an_untyped_fluent_composite_key : given.a_language_service_with_schemas<given.CompositeOrderReadModel>
{
    const string Declaration = """
        projection Order => CompositeOrderReadModel
          from UserAdded
            key OrderKey
              customerId = userId
              orderNumber = name
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserAdded)];

    string _generated;
    string _code;
    Concepts.Projections.PropertyExpression _recompiledKey;

    void Because()
    {
        var compiled = _languageService.Compile(Declaration, Concepts.Projections.ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(value => value, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
        var eventType = compiled.From.Keys.Single();
        compiled.From[eventType] = compiled.From[eventType] with
        {
            Key = "$composite(customerId=userId,orderNumber=name)"
        };
        _generated = _languageService.Generate(compiled, _readModelDefinition);
        _code = new CodeGeneration.CSharp.DeclarativeCodeGenerator().Generate(compiled, _readModelDefinition).ToFullString();
        var recompiled = _languageService.Compile(_generated, Concepts.Projections.ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(value => value, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
        _recompiledKey = recompiled.From[eventType].Key;
    }

    [Fact] void should_get_the_type_from_the_read_model_schema() => _generated.ShouldContain("key OrderKey");
    [Fact] void should_keep_both_properties_in_the_recompiled_key() => _recompiledKey.Value.ShouldContain("customerId=userId, orderNumber=name");
    [Fact] void should_use_the_schema_type_in_generated_code() => _code.ShouldContain(".UsingCompositeKey<OrderKey>");
    [Fact] void should_emit_the_first_mapping_in_generated_code() => _code.ShouldContain(".Set(k => k.customerId).To(e => e.userId)");
}
