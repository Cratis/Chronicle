// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_Parser;

public class when_parsing_nested_block_without_from : given.a_parser
{
    const string Declaration = """
        projection Slice => SliceReadModel
          from SliceCreated
            Name = name

          nested command
            clear with CommandClearedForSlice
        """;

    void Because() => Parse(Declaration);

    [Fact] void should_have_errors() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_report_missing_from_directive() => _errors.Errors.ShouldContain(e => e.Message.Contains("nested", StringComparison.OrdinalIgnoreCase) && e.Message.Contains("from", StringComparison.OrdinalIgnoreCase));
}
