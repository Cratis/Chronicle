// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_with_status;

public class and_v2_authentication_fails_after_erasure : given.a_corrupted_v2_value_after_erasure
{
    async Task Because() => _error = await Catch.Exception(() => _handler.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _value));

    [Fact] void should_propagate_corruption_instead_of_classifying_it_as_erased() => _error.ShouldBeOfExactType<AuthenticationTagMismatchException>();
}
