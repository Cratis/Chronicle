// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.AspNetCore.Auditing.for_CausationMiddleware;

public class when_invoking_without_all_header_values_present : given.a_causation_middleware_with_required_properties_present
{
    async Task Because() => await middleware.InvokeAsync(http_context.Object);

    [Fact] void should_not_add_origin_property() => causation_properties.ContainsKey(CausationMiddleware.CausationOriginProperty).ShouldBeFalse();
    [Fact] void should_not_add_referer_property() => causation_properties.ContainsKey(CausationMiddleware.CausationRefererProperty).ShouldBeFalse();
}
