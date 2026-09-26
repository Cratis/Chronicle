// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_generating_a_nested_composite_key : given.a_language_service_with_schemas<given.NestedCompositeReadModel>
{
    const string Declaration = """
        projection Parent => NestedCompositeReadModel
          nested item
            from UserAdded
              key OrderKey
                customerId = userId
                orderNumber = name
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserAdded)];

    string _generated;

    void Because()
    {
        var compiled = _languageService.Compile(Declaration, Concepts.Projections.ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(value => value, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
        _generated = _languageService.Generate(compiled, _readModelDefinition);
    }

    [Fact] void should_keep_the_nested_key_type_in_the_declaration() => _generated.ShouldContain("key OrderKey");
}
