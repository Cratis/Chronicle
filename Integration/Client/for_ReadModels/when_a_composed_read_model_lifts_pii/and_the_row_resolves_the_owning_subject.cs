// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii.and_the_row_resolves_the_owning_subject.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii;

/// <summary>
/// The single-subject path a read model has always had: the row's own <c>Id</c> is the person the value
/// belongs to, so it decrypts. A control for the whole undeclared release path, at the tier where the value
/// is genuinely ciphertext — this is what must not move.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_row_resolves_the_owning_subject(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.two_stored_subjects(fixture)
    {
        public MisKeyedDueSubject? Released { get; private set; }

        async Task Because()
        {
            await StoreBothSubjects();

            if (!DocumentsCanBeInspected)
            {
                return;
            }

            Released = await EventStore.ReadModels.Release(new MisKeyedDueSubject(FirstPerson.Value, FirstCommentAtRest));
        }
    }

    [Fact] void should_have_stored_the_comment_encrypted() => (!Context.DocumentsCanBeInspected || Context.FirstCommentAtRest != Context.FirstEvent.Comment.Value).ShouldBeTrue();
    [Fact] void should_release_the_comment() => (!Context.DocumentsCanBeInspected || Context.Released!.Comment.Value == Context.FirstEvent.Comment.Value).ShouldBeTrue();
}

#pragma warning restore SA1402
