// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class with_several_matching_properties : for_LanguageService.given.a_language_service_with_qualified_read_models
{
    void Establish()
    {
        _readModels[0].GetSchemaForLatestGeneration().Properties["NAME"] = new JsonSchemaProperty { Type = JsonObjectType.String };
        _eventTypes[0].Schema.Properties["NAME"] = new JsonSchemaProperty { Type = JsonObjectType.String };
    }

    void Because() => CompileExpectingErrors("""
        projection Orders => Example.ReadModels.Order
          from OrderPlaced
            name = Name
            Name = name
        """);

    [Fact] void should_list_the_target_candidates() => _errors.Errors.Select(_ => _.Message).ShouldContain("Read model property 'name' not found; did you mean one of 'NAME', 'Name'?");
    [Fact] void should_list_the_source_candidates() => _errors.Errors.Select(_ => _.Message).ShouldContain("Event property 'name' not found; did you mean one of 'NAME', 'Name'?");
}
