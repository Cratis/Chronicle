// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.for_ProjectionExpressions.when_reading_a_value;

public class that_is_a_quoted_number : Specification
{
    ProjectionValueSource _result = null!;

    void Because() => _result = ProjectionExpressions.ReadValue("\"1\"");

    [Fact] void should_read_it_as_text() => _result.Kind.ShouldEqual(ProjectionValueKind.Text);

    [Fact] void should_keep_what_was_between_the_quotes() => _result.Value.ShouldEqual("1");
}
