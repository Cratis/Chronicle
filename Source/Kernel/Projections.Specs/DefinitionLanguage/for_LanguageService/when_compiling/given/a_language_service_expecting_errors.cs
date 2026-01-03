// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling.given;

public class a_language_service_expecting_errors : Specification
{
    protected ILanguageService _languageService;
    protected CompilerErrors _errors;

    void Establish()
    {
        _languageService = new LanguageService(new Generator());
    }

    protected void Compile(string definition)
    {
        var result = _languageService.Compile(
            definition,
            ProjectionOwner.Client,
            [],
            []);

        _errors = result.Match(
            _ => CompilerErrors.Empty,
            errors => errors);
    }
}
