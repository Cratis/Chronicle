// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii.and_the_declared_subject_owns_no_key.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii;

/// <summary>
/// The honest limit of the declaration: it says which subject a value belongs to, it does not make a wrong
/// answer right. Pointed at an identity that never encrypted anything, the release runs under that subject
/// and the value degrades to empty exactly as it would have under a wrongly inferred one.
/// <para>
/// What changed is that the outcome is now the consequence of something the read model said, rather than of
/// whether one of its properties happened to be named <c>Id</c>.
/// </para>
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_declared_subject_owns_no_key(context context) : Given<context>(context)
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

            Released = await EventStore.ReadModels.Release(new DeclaredDueSubject("composed-pii-never-a-person", FirstCommentAtRest));
        }
    }

    [Fact] void should_not_release_the_comment() => (!Context.DocumentsCanBeInspected || Context.Released!.Comment.Value != Context.FirstEvent.Comment.Value).ShouldBeTrue();
    [Fact] void should_degrade_the_comment_to_empty() => (!Context.DocumentsCanBeInspected || Context.Released!.Comment.Value.Length == 0).ShouldBeTrue();
}

#pragma warning restore SA1402
