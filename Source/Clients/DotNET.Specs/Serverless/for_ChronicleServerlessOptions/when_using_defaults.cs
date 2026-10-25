// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ChronicleServerlessOptions;

public class when_using_defaults : Specification
{
    readonly ChronicleServerlessOptions _options = new();
    [Fact] void should_limit_received_bodies_to_sixteen_megabytes() => _options.Delivery.MaximumBodySize.ShouldEqual(16L * 1024 * 1024);
}
