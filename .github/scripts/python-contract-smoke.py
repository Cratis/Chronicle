#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Consumer smoke for an installed cratis-chronicle-contracts wheel. Run it from a clean virtual environment."""

from __future__ import annotations

import sys
from importlib import import_module
from importlib.metadata import version

expected_version = sys.argv[1]
installed = version("cratis-chronicle-contracts")
assert installed == expected_version, f"installed {installed}, expected {expected_version}"

package = "cratis_chronicle_contracts"
bcl = import_module(f"{package}.protobuf_net.bcl_pb2")
guid = bcl.Guid(lo=1, hi=2)
assert (guid.lo, guid.hi) == (1, 2)
assert import_module(f"{package}.protobuf_net.bcl_pb2").Guid.FromString(guid.SerializeToString()) == guid

event_types = import_module(f"{package}.eventtypes_pb2")
assert event_types.EventType(Id="example", Generation=1).Id == "example"
assert hasattr(import_module(f"{package}.eventtypes_pb2_grpc"), "EventTypesStub")

# Event sources: the contract this distribution exists to provide.
event_sources = import_module(f"{package}.eventsources_pb2")
assert hasattr(import_module(f"{package}.eventsources_pb2_grpc"), "EventSourcesStub")
definition = event_sources.EventSourceDefinition(
    Name="orders",
    Owner=event_sources.Client,
    Concurrency=event_sources.EventSourceId | event_sources.EventStreamType,
    Streams=[event_sources.EventStreamDefinition(Name="main")],
)
assert event_sources.EventSourceDefinition.FromString(definition.SerializeToString()) == definition
request = event_sources.RegisterEventSourcesRequest(EventStore="store", Sources=[definition])
assert request.Sources[0].Streams[0].Name == "main"

# Appending must carry the event source type and event stream fields.
sequences = import_module(f"{package}.sequences_pb2")
assert hasattr(import_module(f"{package}.sequences_pb2_grpc"), "EventSequencesStub")
append = sequences.AppendRequest(
    EventStore="store", EventSourceId="id", EventSourceType="Order", EventStreamType="Main", EventStreamId="s1"
)
assert sequences.AppendRequest.FromString(append.SerializeToString()) == append
fields = {field.name for field in sequences.AppendRequest.DESCRIPTOR.fields}
for required in ("EventSourceType", "EventStreamType", "EventStreamId", "EventSource", "ConcurrencyScope"):
    assert required in fields, f"{required} missing from AppendRequest"

print(f"cratis-chronicle-contracts {installed} smoke passed on Python {sys.version.split()[0]}")
