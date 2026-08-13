// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii.and_the_owning_subject_is_declared.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii;

/// <summary>
/// The same row as <see cref="and_nothing_is_declared"/> — no <c>Id</c>, no <c>[Subject]</c>, so no
/// compliance subject of its own — with one line added saying which subject the lifted comment belongs to.
/// The release pass now runs, under that subject, and the advisor sees the reason instead of a base64 blob.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_owning_subject_is_declared(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.two_stored_subjects(fixture)
    {
        public DeclaredDueSubject? Released { get; private set; }

        async Task Because()
        {
            await StoreBothSubjects();

            if (!DocumentsCanBeInspected)
            {
                return;
            }

            Released = await EventStore.ReadModels.Release(new DeclaredDueSubject(FirstPerson.Value, FirstCommentAtRest));
        }
    }

    [Fact] void should_have_stored_the_comment_encrypted() => (!Context.DocumentsCanBeInspected || Context.FirstCommentAtRest != Context.FirstEvent.Comment.Value).ShouldBeTrue();
    [Fact] void should_release_the_comment_under_the_declared_subject() => (!Context.DocumentsCanBeInspected || Context.Released!.Comment.Value == Context.FirstEvent.Comment.Value).ShouldBeTrue();
    [Fact] void should_keep_the_declared_subject_property() => (!Context.DocumentsCanBeInspected || Context.Released!.SubjectId.Value == Context.FirstPerson.Value).ShouldBeTrue();
}

/// <summary>
/// The composed row, saying which subject the comment it lifted belongs to.
/// </summary>
/// <param name="SubjectId">The person the row is about.</param>
/// <param name="Comment">The comment lifted off that person's own stored row.</param>
public record DeclaredDueSubject(
    SubjectIdentifier SubjectId,
    [SubjectFrom(nameof(SubjectId))] PostponementComment Comment);

#pragma warning restore SA1402
