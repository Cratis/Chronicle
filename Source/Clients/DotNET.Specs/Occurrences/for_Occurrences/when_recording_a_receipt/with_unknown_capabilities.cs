// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences.for_Occurrences.when_recording_a_receipt;

public class with_unknown_capabilities : given.namespace_bound_occurrences
{
    void Establish() => ((IKernelCapabilities)_connection).CapabilitiesAreKnown.Returns(false);

    async Task Because() => _error = await Catch.Exception(() => _occurrences.RecordEffectReceipt(_key, new NoEvents()));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<EffectReceiptsNotSupported>();
    [Fact] void should_not_send_a_receipt_to_an_unknown_kernel() => _service.DidNotReceive().RecordEffectReceipt(Arg.Any<Contracts.Occurrences.RecordEffectReceiptRequest>(), CallContext.Default);
}
