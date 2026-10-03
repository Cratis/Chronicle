# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

import re
from importlib import import_module
from pathlib import Path

# The event type contract is generated from the artifacts in Core, which put it under EventTypes rather than the
# Events namespace it used to share with the constraints - so the module it lands in is eventtypes_pb2.
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


def test_supported_python_floor_matches_idiomatic_clients() -> None:
    # The idiomatic Python client supports 3.10 through 3.14, so the contracts it depends on must install there too.
    # Parsed with a regular expression because tomllib is not available on Python 3.10.
    pyproject = (Path(__file__).parent.parent / "pyproject.toml").read_text(encoding="utf-8")

    assert re.search(r'^requires-python = ">=3\.10"$', pyproject, re.MULTILINE)
    for version in ("3.10", "3.11", "3.12", "3.13", "3.14"):
        assert f'"Programming Language :: Python :: {version}"' in pyproject
    assert 'target-version = "py310"' in pyproject


def test_messages_and_services_round_trip() -> None:
    event_type = eventtypes_pb2.EventType(Id="example", Generation=2)
    parsed = eventtypes_pb2.EventType.FromString(event_type.SerializeToString())
    guid = bcl_pb2.Guid.FromString(bcl_pb2.Guid(lo=3, hi=4).SerializeToString())

    assert (parsed.Id, parsed.Generation) == ("example", 2)
    assert (guid.lo, guid.hi) == (3, 4)
