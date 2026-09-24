// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedMetadataProvider.when_asking_if_can_provide_for_type;

public class and_type_is_a_value_object_adorned_with_encrypted : given.a_provider
{
    [Encrypted]
    record WebhookCredentials(string ClientId, string ClientSecret);

    bool _result;

    void Because() => _result = provider.CanProvide(typeof(WebhookCredentials));

    [Fact] void should_be_able_to_provide() => _result.ShouldBeTrue();
}
