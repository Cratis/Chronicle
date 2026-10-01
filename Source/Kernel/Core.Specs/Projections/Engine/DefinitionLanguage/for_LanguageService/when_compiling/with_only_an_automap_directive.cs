// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

/// <summary>
/// Screenplay reports a projection without a block as an error. Chronicle counts <c language="csharp">automap</c> as a directive, so the
/// relaxation of that diagnostic has to keep applying to it.
/// </summary>
public class with_only_an_automap_directive : given.a_language_service_expecting_errors
{
    const string Declaration = """
        projection Account => AccountReadModel
          automap
        """;

    void Because() => Compile(Declaration);

    [Fact] void should_not_have_errors() => _errors.HasErrors.ShouldBeFalse();
}
