// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_HealthOnlyPortPolicy.when_the_health_port_is_exclusive;

/// <summary>
/// A percent-encoded space decodes into the request path before this policy sees it, so "/health%20" arrives
/// as "/health ". Routing treats that as a different path and sends it to the fallback, which serves the
/// Workbench entry document - so forgiving the whitespace here would admit onto the port the very thing the
/// port exists to keep off it. The policy must reject anything routing would not send to the health endpoint.
/// </summary>
public class and_the_health_path_is_padded_with_whitespace : given.an_exclusive_dedicated_health_port
{
    bool _trailingSpace;
    bool _leadingSpace;
    bool _trailingTab;
    bool _spaceAfterTrailingSlash;

    void Because()
    {
        _trailingSpace = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/health ");
        _leadingSpace = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, " /health");
        _trailingTab = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/health\t");
        _spaceAfterTrailingSlash = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/health/ ");
    }

    [Fact] void should_reject_a_trailing_space() => _trailingSpace.ShouldBeTrue();
    [Fact] void should_reject_a_leading_space() => _leadingSpace.ShouldBeTrue();
    [Fact] void should_reject_a_trailing_tab() => _trailingTab.ShouldBeTrue();
    [Fact] void should_reject_a_space_after_a_trailing_slash() => _spaceAfterTrailingSlash.ShouldBeTrue();
}
