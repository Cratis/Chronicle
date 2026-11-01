// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRegistrationPrecedence.given;

public class a_versioned_owner : Specification
{
    protected ConnectedClient _incoming;
    protected ObserverSubscription _owning;
    protected RegistrationPrecedence _result;

    void Establish()
    {
        _incoming = new ConnectedClient { ClientType = ".NET", Version = "2.0.0" };
        _owning = new ObserverSubscription("the-observer", new ObserverKey("the-observer", "the-store", "the-namespace", "the-owner-sequence"), [], typeof(NullObserverSubscriber), SiloAddress.Zero)
        {
            Targets = [new(SiloAddress.Zero, new ConnectedClient { ClientType = ".NET", Version = "1.0.0" })]
        };
    }
}
