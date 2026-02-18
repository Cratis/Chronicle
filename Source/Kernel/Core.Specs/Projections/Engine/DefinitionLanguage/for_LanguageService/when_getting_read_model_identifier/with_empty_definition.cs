// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.given;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.when_getting_read_model_identifier;

public class with_empty_definition : a_language_service
{
    const string Declaration = "";

    CompilerErrors _errors;

    void Because()
    {
        var result = _languageService.GetReadModelIdentifier(Declaration);
        _errors = result.Match(
            identifier => throw new InvalidOperationException("Should have failed"),
            errors => errors);
    }

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
}
