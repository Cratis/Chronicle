// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.EventTypes;

namespace Cratis.Chronicle.Discovery.for_EventTypeIdentityDrift.when_finding.given;

public class a_renamed_event_type : Specification
{
    protected EventTypeDetailsResponse[] _stored;
    protected DiscoveryReport _current;
    protected string _schema;
    protected ArtifactIdentityDrift[] _result;

    void Establish()
    {
        _stored = [new() { Type = new() { Id = "Original" }, Owner = EventTypeOwner.Client, Schema = """{"title":"Original","type":"object","properties":{"name":{"type":"string"},"age":{"type":"number"}}}""" }];
        _schema = """{"title":"Renamed","type":"object","properties":{"name":{"type":"string"},"age":{"type":"number"}}}""";
        _current = new(true, ImmutableList.Create(new DiscoveredArtifact(ArtifactKind.EventType, "Renamed", null)));
    }
}
