// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationServeClient;

public class when_serving_is_off : given.a_serve_client
{
    void Because()
    {
        _serving = false;
        Start();
    }

    [Fact] void should_not_open_the_serve_call() => _serve.Calls.ShouldEqual(0);
}
