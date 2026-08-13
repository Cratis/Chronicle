// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii.and_the_row_carries_two_subjects.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii;

/// <summary>
/// A row holding two people's personal data, each encrypted under their own key by a real kernel. One
/// subject per read model could never release both — whichever subject the row resolved, the other person's
/// value came back empty. Declaring each value's own subject is what makes the shape expressible.
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_row_carries_two_subjects(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.two_stored_subjects(fixture)
    {
        public MultiSubjectRow? Released { get; private set; }

        async Task Because()
        {
            await StoreBothSubjects();

            if (!DocumentsCanBeInspected)
            {
                return;
            }

            Released = await EventStore.ReadModels.Release(new MultiSubjectRow(
                FirstPerson.Value,
                SecondPerson.Value,
                FirstCommentAtRest,
                SecondCommentAtRest));
        }
    }

    [Fact] void should_have_stored_both_comments_encrypted() => (!Context.DocumentsCanBeInspected || (Context.FirstCommentAtRest != Context.FirstEvent.Comment.Value && Context.SecondCommentAtRest != Context.SecondEvent.Comment.Value)).ShouldBeTrue();
    [Fact] void should_have_stored_two_different_ciphertexts() => (!Context.DocumentsCanBeInspected || Context.FirstCommentAtRest != Context.SecondCommentAtRest).ShouldBeTrue();
    [Fact] void should_release_the_first_persons_comment() => (!Context.DocumentsCanBeInspected || Context.Released!.FirstComment.Value == Context.FirstEvent.Comment.Value).ShouldBeTrue();
    [Fact] void should_release_the_second_persons_comment() => (!Context.DocumentsCanBeInspected || Context.Released!.SecondComment.Value == Context.SecondEvent.Comment.Value).ShouldBeTrue();
}

/// <summary>
/// A row about two people, saying which of them each value belongs to.
/// </summary>
/// <param name="FirstPersonId">The first person.</param>
/// <param name="SecondPersonId">The second person.</param>
/// <param name="FirstComment">The first person's comment.</param>
/// <param name="SecondComment">The second person's comment.</param>
public record MultiSubjectRow(
    SubjectIdentifier FirstPersonId,
    SubjectIdentifier SecondPersonId,
    [SubjectFrom(nameof(FirstPersonId))] PostponementComment FirstComment,
    [SubjectFrom(nameof(SecondPersonId))] PostponementComment SecondComment);

#pragma warning restore SA1402
