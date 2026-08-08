// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii.and_nothing_is_declared.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii;

/// <summary>
/// The reported shape, pinned as it stands. The composed row's identity is deliberately not named
/// <c>Id</c>, so no compliance subject resolves, so the release pass returns the instance untouched and the
/// ciphertext the row lifted travels all the way to the caller. Nothing logs and nothing throws.
/// <para>
/// This is a control, and it must stay green on both sides of the change: an undeclared read model behaves
/// exactly as it did. It is also the arrangement's proof of life — if the source value ever stopped being
/// encrypted at rest, every spec in this folder would pass for the wrong reason.
/// </para>
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_nothing_is_declared(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.two_stored_subjects(fixture)
    {
        public UndeclaredDueSubject? Released { get; private set; }

        async Task Because()
        {
            await StoreBothSubjects();

            if (!DocumentsCanBeInspected)
            {
                return;
            }

            Released = await EventStore.ReadModels.Release(new UndeclaredDueSubject(FirstPerson.Value, FirstCommentAtRest));
        }
    }

    [Fact] void should_have_read_the_stored_documents_when_the_backend_allows_it() => (!Context.DocumentsCanBeInspected || Context.FirstCommentAtRest.Length > 0).ShouldBeTrue();
    [Fact] void should_have_stored_the_comment_encrypted() => (!Context.DocumentsCanBeInspected || Context.FirstCommentAtRest != Context.FirstEvent.Comment.Value).ShouldBeTrue();
    [Fact] void should_not_release_the_comment() => (!Context.DocumentsCanBeInspected || Context.Released!.Comment.Value != Context.FirstEvent.Comment.Value).ShouldBeTrue();
    [Fact] void should_hand_back_the_ciphertext_it_was_given() => (!Context.DocumentsCanBeInspected || Context.Released!.Comment.Value == Context.FirstCommentAtRest).ShouldBeTrue();
}

/// <summary>
/// A composed row whose identity is named <c>SubjectId</c> rather than <c>Id</c>, and which says nothing
/// about the comment it lifted.
/// </summary>
/// <param name="SubjectId">The person the row is about.</param>
/// <param name="Comment">The comment lifted off the person's own stored row.</param>
public record UndeclaredDueSubject(SubjectIdentifier SubjectId, PostponementComment Comment);

#pragma warning restore SA1402
