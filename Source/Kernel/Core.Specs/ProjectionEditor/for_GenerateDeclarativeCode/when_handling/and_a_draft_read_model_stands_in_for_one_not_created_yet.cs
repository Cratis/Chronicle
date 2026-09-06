// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ProjectionEditor.for_GenerateDeclarativeCode.when_handling;

/// <summary>
/// The editor generates code for a projection while the read model it targets is still being drafted, so the draft
/// has to reach the compiler as though it were registered.
/// </summary>
public class and_a_draft_read_model_stands_in_for_one_not_created_yet : given.a_declaration_to_generate_from
{
    const string DraftIdentifier = "a-draft-read-model";

    void Establish() => Compiles(DraftIdentifier);

    async Task Because() =>
        await new GenerateDeclarativeCode(
            EventStore,
            "Default",
            Declaration,
            new DraftReadModel(DraftIdentifier, "A Draft", "drafts", string.Empty)).Handle(_storage, _languageService);

    [Fact] void should_offer_the_draft_to_the_compiler() =>
        _languageService.Received(1).Compile(
            Arg.Any<string>(),
            Arg.Any<Concepts.Projections.ProjectionOwner>(),
            Arg.Is<IEnumerable<ReadModelDefinition>>(_ => _.Any(readModel => readModel.Identifier == DraftIdentifier)),
            Arg.Any<IEnumerable<Concepts.EventTypes.EventTypeSchema>>());
}
