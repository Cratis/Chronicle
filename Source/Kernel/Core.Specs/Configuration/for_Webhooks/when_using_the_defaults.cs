// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Configuration.for_Webhooks;

public class when_using_the_defaults : Specification
{
    Webhooks _options;

    void Because() => _options = new();

#if DEVELOPMENT
    [Fact] void should_warn_about_destinations() => _options.Validation.ShouldEqual(WebhookValidationMode.Warn);
#else
    [Fact] void should_block_local_destinations() => _options.Validation.ShouldEqual(WebhookValidationMode.BlockLocalAddresses);
#endif
}
