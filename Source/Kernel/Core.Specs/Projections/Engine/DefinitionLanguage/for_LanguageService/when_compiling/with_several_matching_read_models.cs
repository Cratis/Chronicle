// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService.when_compiling;

public class with_several_matching_read_models : for_LanguageService.given.a_language_service_with_qualified_read_models
{
    void Establish() => _readModels.Add(CreateReadModel("Other.ReadModels.Order"));

    void Because() => CompileExpectingErrors("projection Orders => OrderSummary\n  from OrderPlaced\n");

    [Fact] void should_reject_the_ambiguous_name() => _errors.HasErrors.ShouldBeTrue();
    [Fact] void should_list_the_identifiers() => _errors.Errors.Single().Message.ShouldEqual($"Read model 'OrderSummary' not found; did you mean one of '{ReadModelName}', 'Other.ReadModels.Order'?");
}
