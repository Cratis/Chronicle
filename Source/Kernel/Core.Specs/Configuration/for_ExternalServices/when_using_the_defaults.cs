// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Configuration.for_ExternalServices;

public class when_using_the_defaults : Specification
{
    ExternalServices _options;

    void Because() => _options = new();

#if DEVELOPMENT
    [Fact] void should_warn_about_destinations() => _options.Validation.ShouldEqual(WebhookValidationMode.Warn);
#else
    [Fact] void should_block_local_destinations() => _options.Validation.ShouldEqual(WebhookValidationMode.BlockLocalAddresses);
#endif
    [Fact] void should_not_allow_insecure_http() => _options.AllowInsecureHttp.ShouldBeFalse();
    [Fact] void should_have_no_allowed_hosts() => _options.AllowedHosts.ShouldBeEmpty();
    [Fact] void should_have_no_event_store_overrides() => _options.EventStoreAllowedHosts.ShouldBeEmpty();
}
