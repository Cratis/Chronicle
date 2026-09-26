// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_generating_from_a_nullable_composite_key : given.a_language_service_with_client_schemas<given.NullableCompositeOrderReadModel>
{
    const string Declaration = """
        projection Order => NullableCompositeOrderReadModel
          from UserAdded
            key OrderKey
              customerId = userId
              orderNumber = name
        """;

    string _fluentExpression;
    string _generated;
    Concepts.Projections.PropertyExpression _recompiledKey;
    IDictionary<string, object> _originalResolvedKey;
    IDictionary<string, object> _recompiledResolvedKey;

    async Task Because()
    {
        var fluentKey = new Client::Cratis.Chronicle.Projections.CompositeKeyBuilder<given.OrderKey, given.UserAdded>(new CamelCaseNamingPolicy());
        fluentKey.Set(key => key.CustomerId).To(@event => @event.UserId);
        fluentKey.Set(key => key.OrderNumber).To(@event => @event.Name);
        _fluentExpression = fluentKey.Build().Value;

        var compiled = _languageService.Compile(Declaration, Concepts.Projections.ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(value => value, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
        var eventType = compiled.From.Keys.Single();
        compiled.From[eventType] = compiled.From[eventType] with { Key = _fluentExpression };
        _generated = _languageService.Generate(compiled, _readModelDefinition);
        var recompiled = _languageService.Compile(_generated, Concepts.Projections.ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(value => value, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
        _recompiledKey = recompiled.From[eventType].Key;
        _originalResolvedKey = await ResolveKey(_fluentExpression, "id");
        _recompiledResolvedKey = await ResolveKey(_recompiledKey.Value, "id");
    }

    [Fact] void should_keep_the_fluent_expression_untyped() => _fluentExpression.ShouldEqual("$composite(customerId=userId,orderNumber=name)");
    [Fact] void should_get_the_nullable_type_from_the_client_read_model_schema() => _generated.ShouldContain("key OrderKey");
    [Fact] void should_keep_the_type_in_the_recompiled_key() => CompositeKeyExpression.Parse(_recompiledKey.Value).TypeName.ShouldEqual(nameof(given.OrderKey));
    [Fact] void should_keep_both_mappings_in_the_recompiled_key() => CompositeKeyExpression.Parse(_recompiledKey.Value).Mappings.SequenceEqual(CompositeKeyExpression.Parse(_fluentExpression).Mappings).ShouldBeTrue();
    [Fact] void should_resolve_the_same_customer_id() => _recompiledResolvedKey["customerId"].ShouldEqual(_originalResolvedKey["customerId"]);
    [Fact] void should_resolve_the_same_order_number() => _recompiledResolvedKey["orderNumber"].ShouldEqual(_originalResolvedKey["orderNumber"]);
}
