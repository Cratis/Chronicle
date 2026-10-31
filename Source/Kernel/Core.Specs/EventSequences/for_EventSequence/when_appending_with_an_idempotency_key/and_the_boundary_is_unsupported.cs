// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_boundary_is_unsupported : given.an_event_sequence_with_idempotency_keys
{
    void Establish() => _complianceManager.LocateProtectedValues(Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Throws(new ProtectedBoundaryUnsupported("boundary"));
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_refuse_the_boundary() => _result.Result.Errors.Single().Value.ShouldEqual("IdempotencyFingerprintUnsupported: Idempotency key 'one' has a protected boundary that cannot be fingerprinted. Nothing was written.");
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
