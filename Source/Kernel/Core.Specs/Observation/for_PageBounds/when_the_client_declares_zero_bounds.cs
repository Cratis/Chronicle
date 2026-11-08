// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_PageBounds;

public class when_the_client_declares_zero_bounds : Specification
{
    PageBounds _bounds;

    void Because() => _bounds = PageBounds.For(new PageDeliveryDefinition(), new Observers());

    [Fact] void should_use_the_kernel_event_maximum() => _bounds.MaxEvents.ShouldEqual(100);
    [Fact] void should_use_the_kernel_byte_maximum() => _bounds.MaxBytes.ShouldEqual(4 * 1024 * 1024);
}
