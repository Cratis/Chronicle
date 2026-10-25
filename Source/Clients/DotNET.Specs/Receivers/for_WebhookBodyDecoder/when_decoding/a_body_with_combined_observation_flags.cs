// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Contracts.Events;
using KernelState = KernelConcepts::Cratis.Chronicle.Concepts.Events.EventObservationState;

namespace Cratis.Chronicle.Receivers.for_WebhookBodyDecoder.when_decoding;

public class a_body_with_combined_observation_flags : given.a_kernel_webhook_body
{
    protected override string Body => Serialize(KernelState.Initial | KernelState.Replay);
    Task Because() => Decode();
    [Fact] void should_decode_successfully() => _error.ShouldBeNull();
    [Fact] void should_preserve_the_kernel_flags() => _delivery.Events.Single().Context.ObservationState.ShouldEqual((EventObservationState)(KernelState.Initial | KernelState.Replay));
}
