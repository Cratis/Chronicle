// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.given;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.when_getting_read_model_identifier;

public class with_valid_definition : a_language_service
{
    const string Declaration = """
        projection MyProjection => MyReadModel
          automap
          from MyEvent key id
        """;

    ReadModelIdentifier _result;

    void Because()
    {
        var result = _languageService.GetReadModelIdentifier(Declaration);
        _result = result.Match(
            identifier => identifier,
            errors => throw new InvalidOperationException($"Failed to get read model identifier: {string.Join(", ", errors.Errors)}"));
    }

    [Fact] void should_return_my_read_model() => _result.Value.ShouldEqual("MyReadModel");
}
