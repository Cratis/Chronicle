// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling.given;

public class a_language_service_expecting_errors : Specification
{
    protected ILanguageService _languageService;
    protected ProjectionId _projectionId;
    protected ParsingErrors _errors;

    void Establish()
    {
        _languageService = new LanguageService(new Generator());
        _projectionId = new ProjectionId(Guid.NewGuid().ToString());
    }

    protected void Compile(string definition)
    {
        var result = _languageService.Compile(
            definition,
            _projectionId,
            ProjectionOwner.Client,
            EventSequenceId.Log);

        _errors = result.Match(
            _ => ParsingErrors.Empty,
            errors => errors);
    }
}
