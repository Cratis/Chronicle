// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.AspNetCore.Receivers.for_MapChronicleReceiver;

public class when_mapping_with_a_custom_path : given.a_receiver_host
{
    protected override string CustomPath => "/hooks/";
    protected override string Path => "/hooks/store/tenant-a/reactor/my-reactor";
    Task Because() => Send();
    [Fact] void should_map_the_custom_base_path() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_forward_the_target() => _target!.ObserverId.ShouldEqual("my-reactor");
}
