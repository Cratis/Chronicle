// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleaser.when_releasing;

/// <summary>
/// A namespace-scoped <c language="csharp">[Encrypted]</c> value is keyed by the real event store and namespace
/// alone - it needs no subject identity at all. A read model carrying only that kind of value ordinarily declares
/// no <c language="csharp">Id</c> or <see cref="SubjectAttribute"/> either, because there is no per-document
/// identity for the value to be scoped by. Release must still reach the kernel for such a read model, rather than
/// treating the absence of a resolvable subject as "nothing to release".
/// </summary>
public class and_a_namespace_scoped_encrypted_value_has_no_subject : given.a_read_model_releaser
{
    given.a_read_model_releaser.WebhookConfiguration _instance;
    given.a_read_model_releaser.WebhookConfiguration _result;

    void Establish()
    {
        GivenSchemaFor<WebhookConfiguration>();
        GivenReleaseEchoesPayload();
        _instance = new WebhookConfiguration("ciphertext");
    }

    async Task Because() => _result = await _releaser.Release(_instance);

    [Fact] void should_have_called_release() => _compliance.Received(1).Release(Arg.Any<Contracts.Compliance.ReleaseRequest>());
    [Fact] void should_have_released_against_subject_not_set() => LastReleaseRequest()!.Subject.ShouldEqual(Subject.NotSet.Value);
    [Fact] void should_hand_back_a_reconstructed_instance() => _result.Secret.ShouldEqual("ciphertext");
}
