// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class a_capture_with_map_block : for_LanguageService.given.a_language_service
{
    const string Declaration = """
        capture Customers
          source api
            api CustomersApi
            poll 5m
          key customerId
          map
            firstName = fornavn
            fullName = `${fornavn} ${etternavn}`
          append CustomerChanged
            when firstName or lastName
            firstName = $.firstName
        """;

    CaptureDefinition _result;

    void Because() => _result = Compile(Declaration);

    [Fact] void should_have_map_definition() => _result.Map.ShouldNotBeNull();
    [Fact] void should_have_field_rename_operation() => ((FieldRenameOperation)_result.Map!.Operations[0]).SourceProperty.ShouldEqual("fornavn");
    [Fact] void should_have_template_assign_operation() => ((TemplateAssignOperation)_result.Map!.Operations[1]).Template.ShouldEqual("${fornavn} ${etternavn}");
}
