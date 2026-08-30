# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

from importlib import import_module

eventtypes_pb2 = import_module("cratis_chronicle_contracts.eventtypes_pb2")
eventtypes_pb2_grpc = import_module("cratis_chronicle_contracts.eventtypes_pb2_grpc")
bcl_pb2 = import_module("cratis_chronicle_contracts.protobuf_net.bcl_pb2")


def test_event_contract_is_importable() -> None:
    event_type = eventtypes_pb2.EventType(Id="example", Generation=1)

    assert event_type.Id == "example"
    assert event_type.Generation == 1


def test_event_service_stub_is_generated() -> None:
    assert hasattr(eventtypes_pb2_grpc, "EventTypesStub")


def test_protobuf_net_contract_is_importable() -> None:
    value = bcl_pb2.Guid(lo=1, hi=2)

    assert value.lo == 1
    assert value.hi == 2
