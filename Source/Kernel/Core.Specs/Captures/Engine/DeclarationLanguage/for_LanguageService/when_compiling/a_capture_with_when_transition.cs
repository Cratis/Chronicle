// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class a_capture_with_when_transition : for_LanguageService.given.a_language_service
{
    const string Declaration = """
        capture Customers
          source api
            api CustomersApi
            poll 5m
          key customerId
          append CustomerActivated
            when status from inactive to active
            status = $.status
        """;

    CaptureDefinition _result;
    WhenClause _when;

    void Because()
    {
        _result = Compile(Declaration);
        _when = _result.Appends[0].When;
    }

    [Fact] void should_have_transition_when_clause() => _when.Type.ShouldEqual(WhenClauseType.ValueTransition);
    [Fact] void should_have_property() => _when.Properties[0].ShouldEqual("status");
    [Fact] void should_have_from_value() => _when.FromValue.ShouldEqual("inactive");
    [Fact] void should_have_to_value() => _when.ToValue.ShouldEqual("active");
}
