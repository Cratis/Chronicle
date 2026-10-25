// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Receivers.for_MapChronicleReceiver;

public class when_posting_with_an_invalid_observer_type : given.a_receiver_host
{
    protected override string Path => "/chronicle/deliveries/store/tenant-a/99/my-reactor";
    Task Because() => Send();
    [Fact] void should_forward_a_null_target() => _target.ShouldBeNull();
    [Fact] void should_still_forward_the_body() => _body.SequenceEqual(CreateRequestBody()).ShouldBeTrue();
}
