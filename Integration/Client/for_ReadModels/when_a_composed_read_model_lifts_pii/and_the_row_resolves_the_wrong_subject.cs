// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii.and_the_row_resolves_the_wrong_subject.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii;

/// <summary>
/// The other outcome available to an undeclared composed row, and the reason renaming the identity to
/// <c>Id</c> is not the repair it looks like: the row now resolves a subject, but not the one the lifted
/// value belongs to, so the value is decrypted under the wrong key and degrades to empty.
/// <para>
/// A control. It must stay green on both sides — the change adds a way to say which subject a value belongs
/// to, it does not change what happens to a row that says nothing.
/// </para>
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_row_resolves_the_wrong_subject(context context) : Given<context>(context)
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

            // The second person owns an encryption key of their own — they have personal data at rest — so
            // this is a genuine decryption under the wrong key, not the missing-key case.
            Released = await EventStore.ReadModels.Release(new MisKeyedDueSubject(SecondPerson.Value, FirstCommentAtRest));
        }
    }

    [Fact] void should_have_stored_the_comment_encrypted() => (!Context.DocumentsCanBeInspected || Context.FirstCommentAtRest != Context.FirstEvent.Comment.Value).ShouldBeTrue();
    [Fact] void should_not_release_the_comment() => (!Context.DocumentsCanBeInspected || Context.Released!.Comment.Value != Context.FirstEvent.Comment.Value).ShouldBeTrue();
    [Fact] void should_degrade_the_comment_to_empty() => (!Context.DocumentsCanBeInspected || Context.Released!.Comment.Value.Length == 0).ShouldBeTrue();
}

/// <summary>
/// A composed row keyed by somebody other than the person the lifted comment belongs to.
/// </summary>
/// <param name="Id">The identity the row resolves its compliance subject from.</param>
/// <param name="Comment">The comment lifted off another person's stored row.</param>
public record MisKeyedDueSubject(string Id, PostponementComment Comment);

#pragma warning restore SA1402
