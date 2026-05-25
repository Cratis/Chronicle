// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LanguageService.given;

public class a_language_service : Specification
{
    protected ILanguageService _languageService;

    void Establish()
    {
        _languageService = new LanguageService();
    }

    protected CaptureDefinition Compile(string definition)
    {
        var result = _languageService.Compile(definition);

        return result.Match(
            _ => _,
            errors => throw new InvalidOperationException($"Compilation failed: {string.Join(", ", errors.Errors.Select(_ => _.Message))}"));
    }
}
