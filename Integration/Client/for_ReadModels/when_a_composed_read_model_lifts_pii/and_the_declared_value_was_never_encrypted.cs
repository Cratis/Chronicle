// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii.and_the_declared_value_was_never_encrypted.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii;

/// <summary>
/// One row holding both kinds of personal value about the same person: one the query method computed and one
/// it lifted as ciphertext. Both are declared under that person, and both must come back readable — the
/// declaration has to route the encrypted one to a key that can open it without destroying the plaintext one
/// on the way.
/// <para>
/// The row is keyed by a synthetic identity that is nobody and therefore never minted a key, which is what
/// the inference would otherwise pick. That is the shape a never-encrypted value survives today anyway, so
/// the computed half is a control; the ciphertext half beside it is what makes the pair fail if the
/// declaration stops being honored.
/// </para>
/// </summary>
/// <param name="context">The test context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_declared_value_was_never_encrypted(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.two_stored_subjects(fixture)
    {
        public const string Computed = "Assembled at the query edge, never stored";
        public const string SyntheticIdentity = "composed-pii-never-a-person";

        public ComputedRow? Released { get; private set; }

        async Task Because()
        {
            await StoreBothSubjects();

            if (!DocumentsCanBeInspected)
            {
                return;
            }

            Released = await EventStore.ReadModels.Release(new ComputedRow(
                SyntheticIdentity,
                FirstPerson.Value,
                Computed,
                FirstCommentAtRest));
        }
    }

    [Fact] void should_return_the_computed_value_untouched() => (!Context.DocumentsCanBeInspected || Context.Released!.Computed.Value == context.Computed).ShouldBeTrue();
    [Fact] void should_release_the_stored_value_beside_it() => (!Context.DocumentsCanBeInspected || Context.Released!.Stored.Value == Context.FirstEvent.Comment.Value).ShouldBeTrue();
    [Fact] void should_keep_the_synthetic_identity() => (!Context.DocumentsCanBeInspected || Context.Released!.Id == context.SyntheticIdentity).ShouldBeTrue();
}

/// <summary>
/// A row keyed by a synthetic identity that owns no encryption key, carrying one computed and one stored
/// personal value, both declared under the person they are about.
/// </summary>
/// <param name="Id">The synthetic identity the row is keyed by.</param>
/// <param name="SubjectId">The person both values are about.</param>
/// <param name="Computed">The value the query method computed.</param>
/// <param name="Stored">The value the query method lifted as ciphertext.</param>
public record ComputedRow(
    string Id,
    SubjectIdentifier SubjectId,
    [SubjectFrom(nameof(SubjectId))] PostponementComment Computed,
    [SubjectFrom(nameof(SubjectId))] PostponementComment Stored);

#pragma warning restore SA1402
