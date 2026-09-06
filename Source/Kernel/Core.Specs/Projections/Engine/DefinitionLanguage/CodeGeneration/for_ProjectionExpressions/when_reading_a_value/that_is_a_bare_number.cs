// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.for_ProjectionExpressions.when_reading_a_value;

public class that_is_a_bare_number : Specification
{
    ProjectionValueSource _result = null!;

    void Because() => _result = ProjectionExpressions.ReadValue("42");

    [Fact] void should_read_it_as_a_literal() => _result.Kind.ShouldEqual(ProjectionValueKind.Literal);

    [Fact] void should_keep_the_number() => _result.Value.ShouldEqual("42");
}
