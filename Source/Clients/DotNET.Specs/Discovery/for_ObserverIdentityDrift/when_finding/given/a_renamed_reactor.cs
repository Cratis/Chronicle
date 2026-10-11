// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Discovery.for_ObserverIdentityDrift.when_finding.given;

public class a_renamed_reactor : Specification
{
    protected ObserverInformation[] _stored;
    protected DiscoveryReport _current;
    protected ArtifactIdentityDrift[] _result;

    void Establish()
    {
        _stored = [new() { Id = "Original.SendEmail", Type = ObserverType.Reactor, Owner = ObserverOwner.Client }];
        _current = new(true, ImmutableList.Create(new DiscoveredArtifact(ArtifactKind.Reactor, "Renamed.SendEmail", typeof(SendEmail))));
    }

    public class SendEmail;
}
