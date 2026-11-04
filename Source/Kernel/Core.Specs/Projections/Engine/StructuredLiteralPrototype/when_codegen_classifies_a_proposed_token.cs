// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;

namespace Cratis.Chronicle.Projections.Engine.StructuredLiteralPrototype;

/// <summary>
/// Characterizes the CURRENT legacy collision for the proposed #5525 token: code generation classifies it as an event property.
/// </summary>
public class when_codegen_classifies_a_proposed_token : Specification
{
    const string Token = "$structured:v1(e30)";

    ProjectionValueSource _result = null!;

    void Because() => _result = ProjectionExpressions.ReadValue(Token);

    [Fact] void should_classify_it_as_an_event_property_in_current_legacy_behavior() => _result.Kind.ShouldEqual(ProjectionValueKind.EventProperty);

    [Fact] void should_keep_the_whole_token() => _result.Value.ShouldEqual(Token);
}
