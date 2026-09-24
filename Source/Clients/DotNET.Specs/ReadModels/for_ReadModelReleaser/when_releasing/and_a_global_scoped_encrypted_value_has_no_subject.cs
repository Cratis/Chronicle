// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleaser.when_releasing;

/// <summary>
/// Same reasoning as <see cref="and_a_namespace_scoped_encrypted_value_has_no_subject"/>, one scope wider: a
/// global-scoped <c language="csharp">[Encrypted]</c> value is keyed across the whole installation, independent of
/// event store, namespace, or subject - the ordinary shape for a license token or similar installation-wide secret.
/// </summary>
public class and_a_global_scoped_encrypted_value_has_no_subject : given.a_read_model_releaser
{
    given.a_read_model_releaser.LicenseInformation _instance;
    given.a_read_model_releaser.LicenseInformation _result;

    void Establish()
    {
        GivenSchemaFor<LicenseInformation>();
        GivenReleaseEchoesPayload();
        _instance = new LicenseInformation("ciphertext");
    }

    async Task Because() => _result = await _releaser.Release(_instance);

    [Fact] void should_have_called_release() => _compliance.Received(1).Release(Arg.Any<Contracts.Compliance.ReleaseRequest>());
    [Fact] void should_have_released_against_subject_not_set() => LastReleaseRequest()!.Subject.ShouldEqual(Subject.NotSet.Value);
    [Fact] void should_hand_back_a_reconstructed_instance() => _result.Token.ShouldEqual("ciphertext");
}
