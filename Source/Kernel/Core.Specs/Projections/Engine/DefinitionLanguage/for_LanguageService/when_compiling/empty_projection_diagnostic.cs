// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

/// <summary>
/// The relaxation for projections that declare <c language="csharp">automap</c> or <c language="csharp">sequence</c> instead of a block matches the diagnostic by its
/// code. This pins the code to what the Screenplay compiler reports, so a package bump that renumbers it fails here
/// rather than silently turning the relaxation off.
/// </summary>
public class empty_projection_diagnostic : Specification
{
    const string Declaration = """
        projection Account => AccountReadModel
        """;

    string[] _codes;

    void Because() => _codes = [.. new ScreenplayCompiler().CompileProjection(Declaration).Diagnostics.Select(_ => _.Code)];

    [Fact] void should_report_the_missing_directives_code() => _codes.ShouldContain(LanguageService.MissingDirectivesDiagnosticCode);
}
