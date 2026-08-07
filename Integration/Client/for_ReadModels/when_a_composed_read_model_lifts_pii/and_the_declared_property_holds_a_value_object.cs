// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii.and_the_declared_property_holds_a_value_object.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii;

/// <summary>
/// The declaration sits on the read model's own property, but the personal value is one level down inside a
/// value object — which is where a compliance marker on a composite type actually lands. The whole value
/// object has to travel to the declared subject, and come back with its shape intact and its non-personal
/// sibling untouched.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_declared_property_holds_a_value_object(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.two_stored_subjects(fixture)
    {
        public DeclaredNestedRow? Released { get; private set; }

        async Task Because()
        {
            await StoreBothSubjects();

            if (!DocumentsCanBeInspected)
            {
                return;
            }

            Released = await EventStore.ReadModels.Release(new DeclaredNestedRow(
                FirstPerson.Value,
                new ReviewNote(FirstNoteTextAtRest, FirstEvent.Note.Author)));
        }
    }

    [Fact] void should_have_stored_the_nested_text_encrypted() => (!Context.DocumentsCanBeInspected || Context.FirstNoteTextAtRest != Context.FirstEvent.Note.Text.Value).ShouldBeTrue();
    [Fact] void should_release_the_nested_text() => (!Context.DocumentsCanBeInspected || Context.Released!.Note.Text.Value == Context.FirstEvent.Note.Text.Value).ShouldBeTrue();
    [Fact] void should_keep_the_non_personal_sibling() => (!Context.DocumentsCanBeInspected || Context.Released!.Note.Author == Context.FirstEvent.Note.Author).ShouldBeTrue();
}

/// <summary>
/// A composed row whose declared property holds a value object with a personal value inside it.
/// </summary>
/// <param name="SubjectId">The person the row is about.</param>
/// <param name="Note">The review note lifted off that person's own stored row.</param>
public record DeclaredNestedRow(
    SubjectIdentifier SubjectId,
    [ReleaseUnder(nameof(SubjectId))] ReviewNote Note);

#pragma warning restore SA1402
