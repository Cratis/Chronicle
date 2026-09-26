// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.for_ProjectionExpressions.when_reading_a_value;

public class that_looks_like_an_invalid_literal : Specification
{
    ProjectionValueSource[] _values;

    void Because() => _values = new[] { "nan", "infinity", "true ", "TRUE", "1e400" }.Select(ProjectionExpressions.ReadValue).ToArray();

    [Fact] void should_treat_all_values_as_event_content() => _values.All(_ => _.Kind == ProjectionValueKind.EventProperty).ShouldBeTrue();
}
