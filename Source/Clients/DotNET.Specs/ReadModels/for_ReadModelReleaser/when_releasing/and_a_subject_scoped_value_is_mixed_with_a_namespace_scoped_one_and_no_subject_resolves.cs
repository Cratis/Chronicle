// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleaser.when_releasing;

/// <summary>
/// The boundary between <see cref="and_a_namespace_scoped_encrypted_value_has_no_subject"/> and the pinned
/// no-op for a subject-scoped-only schema (<c language="csharp">Cratis.Chronicle.ReadModels.for_ReadModels.when_releasing.and_no_subject_resolves_and_nothing_is_declared</c>):
/// a schema carrying <em>both</em> a subject-scoped value and a namespace-/global-scoped one, on a read model that
/// resolves no subject. The presence of the namespace-scoped value is what tips this into "release must run" -
/// leaving the actual per-property degradation of the un-keyable PII value to the kernel.
/// </summary>
public class and_a_subject_scoped_value_is_mixed_with_a_namespace_scoped_one_and_no_subject_resolves : given.a_read_model_releaser
{
    given.a_read_model_releaser.MixedProtection _instance;

    void Establish()
    {
        GivenSchemaFor<MixedProtection>();
        GivenReleaseEchoesPayload();
        _instance = new MixedProtection("ciphertext-notes", "ciphertext-secret");
    }

    async Task Because() => await _releaser.Release(_instance);

    [Fact] void should_have_called_release() => _compliance.Received(1).Release(Arg.Any<Contracts.Compliance.ReleaseRequest>());
    [Fact] void should_have_released_against_subject_not_set() => LastReleaseRequest()!.Subject.ShouldEqual(Subject.NotSet.Value);
}
