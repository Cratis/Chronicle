// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.given;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_getting_read_model_identifier;

public class without_explicit_read_model : a_language_service
{
    const string Declaration = """
        projection MyProjection
          automap
          from MyEvent key id
        """;

    ReadModelIdentifier _result;

    void Because()
    {
        var result = _languageService.GetReadModelIdentifier(Declaration);
        _result = result.Match(
            identifier => identifier,
            errors => throw new InvalidOperationException($"Unexpected errors: {string.Join(", ", errors.Errors)}"));
    }

    [Fact] void should_return_inferred_read_model_identifier() => _result.ShouldEqual(ReadModelIdentifier.Inferred);
}
