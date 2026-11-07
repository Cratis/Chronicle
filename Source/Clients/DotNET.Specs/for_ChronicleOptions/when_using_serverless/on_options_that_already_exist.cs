// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Serverless;

namespace Cratis.Chronicle.for_ChronicleOptions.when_using_serverless;

public class on_options_that_already_exist : Specification
{
    ChronicleOptions _options;
    ChronicleOptions _result;

    void Establish() => _options = new ChronicleOptions { SkipCompatibilityCheck = true };

    void Because() => _result = _options.UseServerless("orders");

    [Fact] void should_return_the_same_instance() => _result.ShouldBeSame(_options);
    [Fact] void should_keep_prior_settings() => _options.SkipCompatibilityCheck.ShouldBeTrue();
    [Fact] void should_turn_serverless_on() => _options.Serverless.Mode.ShouldEqual(ServerlessMode.Serverless);
    [Fact] void should_leave_the_receiver_url_unset() => _options.Serverless.ReceiverUrl.ShouldBeNull();
    [Fact] void should_skip_keep_alive() => _options.SkipKeepAlive.ShouldBeTrue();
}
