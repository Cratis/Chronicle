// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.for_ProjectionExpressions.when_reading_a_value;

public class that_is_a_property_path : Specification
{
    ProjectionValueSource _result = null!;

    void Because() => _result = ProjectionExpressions.ReadValue("Owner.Name");

    [Fact] void should_read_it_as_an_event_property() => _result.Kind.ShouldEqual(ProjectionValueKind.EventProperty);

    [Fact] void should_keep_the_whole_path() => _result.Value.ShouldEqual("Owner.Name");
}
