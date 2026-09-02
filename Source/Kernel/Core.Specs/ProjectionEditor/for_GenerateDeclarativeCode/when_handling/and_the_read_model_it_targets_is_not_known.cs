// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor.for_GenerateDeclarativeCode.when_handling;

/// <summary>
/// A declaration can compile and still name a read model the event store does not have. That is something the
/// editor points at, not an exception.
/// </summary>
public class and_the_read_model_it_targets_is_not_known : given.a_declaration_to_generate_from
{
    GeneratedCodeResult _result;

    void Establish() => Compiles("a-read-model-nobody-registered");

    async Task Because() => _result = await new GenerateDeclarativeCode(EventStore, "Default", Declaration).Handle(_storage, _languageService);

    [Fact] void should_not_generate_any_code() => _result.Code.ShouldBeEmpty();
    [Fact] void should_report_the_read_model_it_could_not_find() =>
        _result.Errors.Single().Message.ShouldEqual("Read model 'a-read-model-nobody-registered' not found");
}
