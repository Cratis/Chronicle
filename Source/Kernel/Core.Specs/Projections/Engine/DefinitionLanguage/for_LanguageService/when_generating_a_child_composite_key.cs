// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_generating_a_child_composite_key : given.a_language_service_with_schemas<given.CompositeChildReadModel>
{
    const string Declaration = """
        projection Parent => CompositeChildReadModel
          children items identified by id
            from UserAdded
              key OrderKey
                customerId = userId
                orderNumber = name
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserAdded)];

    string _generated;
    string _code;

    void Because()
    {
        var compiled = _languageService.Compile(Declaration, Concepts.Projections.ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(value => value, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
        var child = compiled.Children.Values.Single();
        var eventType = (EventType)"UserAdded";
        child.From[eventType] = child.From[eventType] with { Key = "$composite(customerId=userId,orderNumber=name)" };
        _generated = _languageService.Generate(compiled, _readModelDefinition);
        _code = new CodeGeneration.CSharp.DeclarativeCodeGenerator().Generate(compiled, _readModelDefinition).ToFullString();
    }

    [Fact] void should_use_the_child_key_type_in_the_declaration() => _generated.ShouldContain("key OrderKey");
    [Fact] void should_use_the_child_key_type_in_code() => _code.ShouldContain(".UsingCompositeKey<OrderKey>");
}
