// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Observation.for_ClientObserversEndpoints.when_handling.given;

public class an_http_context_and_client_observers : Cratis.given.an_http_context
{
    protected Mock<IClientObservers> client_observers;

    void Establish()
    {
        client_observers = new();
        service_provider.Setup(_ => _.GetService(typeof(IClientObservers))).Returns(client_observers.Object);
    }
}
