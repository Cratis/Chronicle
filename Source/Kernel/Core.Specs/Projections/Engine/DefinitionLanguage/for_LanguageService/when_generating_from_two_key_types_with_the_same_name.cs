// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Client;

using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_generating_from_two_key_types_with_the_same_name : given.a_language_service_with_client_schemas<given.DuplicateNamedKeyReadModel>
{
    const string Declaration = """
        projection Order => DuplicateNamedKeyReadModel
          from UserAdded
            key OrderKey
              customerId = userId
              orderNumber = name
        """;

    string _generated;
    Concepts.Projections.PropertyExpression _recompiledKey;

    void Because()
    {
        var fluentKey = new Client::Cratis.Chronicle.Projections.CompositeKeyBuilder<given.OrderKey, given.UserAdded>(new CamelCaseNamingPolicy());
        fluentKey.Set(key => key.CustomerId).To(@event => @event.UserId);
        fluentKey.Set(key => key.OrderNumber).To(@event => @event.Name);

        var compiled = _languageService.Compile(Declaration, Concepts.Projections.ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(value => value, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
        var eventType = compiled.From.Keys.Single();
        compiled.From[eventType] = compiled.From[eventType] with { Key = fluentKey.Build().Value };
        _generated = _languageService.Generate(compiled, _readModelDefinition);
        var recompiled = _languageService.Compile(_generated, Concepts.Projections.ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(value => value, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
        _recompiledKey = recompiled.From[eventType].Key;
    }

    [Fact] void should_generate_the_id_type() => _generated.ShouldContain("key OrderKey");
    [Fact] void should_recompile_against_the_id_shape() => CompositeKeyExpression.Parse(_recompiledKey.Value).Mappings.Count.ShouldEqual(2);
}
