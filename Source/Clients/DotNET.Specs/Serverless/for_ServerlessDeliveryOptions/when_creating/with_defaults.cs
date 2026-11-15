// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessDeliveryOptions.when_creating;

public class with_defaults : Specification
{
    ServerlessDeliveryOptions _result;

    void Because() => _result = new ServerlessDeliveryOptions();

    [Fact] void should_default_the_timeout_to_thirty_seconds() => _result.Timeout.ShouldEqual(TimeSpan.FromSeconds(30));
}
