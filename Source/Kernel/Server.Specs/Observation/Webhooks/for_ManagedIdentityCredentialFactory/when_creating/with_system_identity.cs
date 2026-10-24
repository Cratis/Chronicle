// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Core;
using Azure.Identity;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Server.Observation.Webhooks.for_ManagedIdentityCredentialFactory.when_creating;

public class with_system_identity : Specification
{
    TokenCredential _result;

    void Because() => _result = new ManagedIdentityCredentialFactory(_ => null).Create(ManagedIdentityClientId.NotSet);

    [Fact] void should_choose_the_credential() => _result.ShouldBeOfExactType<ManagedIdentityCredential>();
}
