// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling_unsupported_block.given;

public class a_language_service_compiling_variants : for_LanguageService.given.a_language_service_with_schemas<for_LanguageService.given.CompanyReadModel>
{
    protected CompilerErrors _errors;

    protected override IEnumerable<Type> EventTypes => [typeof(for_LanguageService.given.DepartmentCreated)];

    protected void Compile(string declaration, bool withSchemas)
    {
        var result = _languageService.Compile(
            declaration,
            ProjectionOwner.Client,
            withSchemas ? [_readModelDefinition] : [],
            withSchemas ? _eventTypeSchemas : []);

        _errors = result.Match(_ => CompilerErrors.Empty, errors => errors);
    }
}
