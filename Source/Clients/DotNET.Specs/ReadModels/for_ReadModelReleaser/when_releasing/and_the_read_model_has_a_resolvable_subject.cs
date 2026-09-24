// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReleaser.when_releasing;

/// <summary>
/// Regression: a read model that resolves a real subject from its <c language="csharp">Id</c> property must keep
/// releasing against that subject exactly as before - falling back to <see cref="Subject.NotSet"/> only applies
/// when no subject can be resolved at all.
/// </summary>
public class and_the_read_model_has_a_resolvable_subject : given.a_read_model_releaser
{
    Guid _id;
    given.a_read_model_releaser.Customer _instance;
    given.a_read_model_releaser.Customer _result;

    void Establish()
    {
        GivenSchemaFor<Customer>();
        GivenReleaseEchoesPayload();
        _id = Guid.NewGuid();
        _instance = new Customer(_id, "ciphertext@example.com");
    }

    async Task Because() => _result = await _releaser.Release(_instance);

    [Fact] void should_have_released_against_the_customers_id() => LastReleaseRequest()!.Subject.ShouldEqual(_id.ToString());
    [Fact] void should_hand_back_a_reconstructed_instance() => _result.Email.ShouldEqual("ciphertext@example.com");
}
