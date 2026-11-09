// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Federation.given;

public class a_configured_registry : a_federation_registry
{
    void Establish()
    {
        _options = new()
        {
            Federation = new()
            {
                Bindings = new Dictionary<string, FederationBindingConfiguration>
                {
                    ["orders"] = new()
                    {
                        LogicalSource = "orders-service",
                        Endpoint = "https://peer.example.com:35100",
                        ServerPin = _pin,
                        ClientCertificate = "client-cert",
                        RemoteEventStore = "orders",
                        TargetEventStore = "local-orders"
                    }
                },
                Grants = new Dictionary<string, FederationGrantConfiguration>
                {
                    ["link-1"] = new()
                    {
                        ClientPin = _otherPin,
                        EventStore = "orders",
                        AllPublic = true
                    }
                }
            }
        };
        _registry = CreateRegistry();
    }
}
