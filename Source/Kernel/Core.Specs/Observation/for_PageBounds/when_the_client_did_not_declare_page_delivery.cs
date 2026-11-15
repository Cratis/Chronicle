// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_PageBounds;

public class when_the_client_did_not_declare_page_delivery : Specification
{
    PageBounds _bounds;

    void Because() => _bounds = PageBounds.For(null, new Observers());

    [Fact] void should_not_split_anything() => _bounds.ShouldEqual(PageBounds.Unbounded);
}
