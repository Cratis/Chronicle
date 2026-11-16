// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_strictly;

public class and_v2_authentication_fails_with_recorded_erasure : given.a_corrupted_v2_value_after_erasure
{
    async Task Because() => _error = await Catch.Exception(() => _handler.ReleaseStrict(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _value));

    [Fact] void should_propagate_corruption_instead_of_returning_empty() => _error.ShouldBeOfExactType<AuthenticationTagMismatchException>();
}
