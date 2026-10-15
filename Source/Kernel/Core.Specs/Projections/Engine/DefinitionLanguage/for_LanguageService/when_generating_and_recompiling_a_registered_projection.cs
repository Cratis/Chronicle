// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_generating_and_recompiling_a_registered_projection : given.a_language_service_with_qualified_read_models
{
    ProjectionDefinition _original;
    ProjectionDefinition? _recompiled;
    string _generated;
    ExpandoObject _target;

    void Establish()
    {
        _readModels[0].GetSchemaForLatestGeneration().Title = "Order";
        const string declaration = """
            projection OrderProjection => Example.ReadModels.Order
              from OrderPlaced
                Name = Name
                IsActive = false
                Occurred = $eventContext.Occurred
            """;
        _original = _languageService.Compile(declaration, ProjectionOwner.Client, _readModels, _eventTypes).Match(value => value, _ => null!);
        _original.From[(EventType)"OrderPlaced"].Properties[new PropertyPath("IsActive")] = "$value(False)";
    }

    void Because()
    {
        _generated = _languageService.Generate(_original, _readModels[0]);
        _recompiled = _languageService.Compile(_generated, ProjectionOwner.Client, _readModels, _eventTypes).Match<ProjectionDefinition?>(
            value =>
            {
                _errors = CompilerErrors.Empty;
                return value;
            },
            errors =>
            {
                _errors = errors;
                return null;
            });
        if (_recompiled is null)
        {
            return;
        }

        var typeFormats = new TypeFormats();
        var values = new EventValueProviderExpressionResolvers(typeFormats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var properties = new ReadModelPropertyExpressionResolvers(values, typeFormats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance);
        _target = new ExpandoObject();
        var mapper = properties.Resolve(new PropertyPath("IsActive"), new JsonSchemaProperty { Type = JsonObjectType.Boolean }, _recompiled.From[(EventType)"OrderPlaced"].Properties[new PropertyPath("IsActive")]);
        mapper(null!, _target, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_render_the_registered_identifier() => _generated.ShouldContain($"=> {ReadModelName}");
    [Fact] void should_compile_without_errors() => _errors.Errors.ShouldBeEmpty();
    [Fact] void should_keep_the_read_model_identifier() => _recompiled!.ReadModel.ShouldEqual(_original.ReadModel);
    [Fact] void should_keep_the_projection_identifier() => _recompiled!.Identifier.ShouldEqual(_original.Identifier);
    [Fact] void should_keep_the_event_mappings() => _recompiled!.From[(EventType)"OrderPlaced"].Properties.Where(_ => _.Key.Path != "IsActive").ShouldContainOnly(_original.From[(EventType)"OrderPlaced"].Properties.Where(_ => _.Key.Path != "IsActive"));
    [Fact] void should_keep_the_boolean_value() => ((IDictionary<string, object?>)_target)["IsActive"].ShouldEqual(false);
    [Fact] void should_keep_the_context_path() => _recompiled!.From[(EventType)"OrderPlaced"].Properties[new PropertyPath("Occurred")].ShouldEqual("$eventContext(Occurred)");
}
