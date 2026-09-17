# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

from importlib import import_module

# The event type contract is generated from the artifacts in Core, which put it under EventTypes rather than the
# Events namespace it used to share with the constraints - so the module it lands in is eventtypes_pb2.
eventtypes_pb2 = import_module("cratis_chronicle_contracts.eventtypes_pb2")
eventtypes_pb2_grpc = import_module("cratis_chronicle_contracts.eventtypes_pb2_grpc")
bcl_pb2 = import_module("cratis_chronicle_contracts.protobuf_net.bcl_pb2")
sequences_pb2 = import_module("cratis_chronicle_contracts.sequences_pb2")


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


def test_append_receipts_are_opt_in() -> None:
    single = sequences_pb2.AppendRequest()
    batch = sequences_pb2.AppendManyRequest()
    rich_batch = sequences_pb2.AppendManyForEventSourcesRequest()

    assert not single.IncludeReceipt
    assert not batch.IncludeReceipts
    assert not rich_batch.IncludeReceipts

    single.IncludeReceipt = True
    assert sequences_pb2.AppendRequest.FromString(single.SerializeToString()).IncludeReceipt


def test_absent_receipt_remains_absent() -> None:
    response = sequences_pb2.AppendResponse(IsSuccess=False)
    restored = sequences_pb2.AppendResponse.FromString(response.SerializeToString())

    assert not restored.HasField("Receipt")


def test_persisted_metadata_survives_a_receipt_roundtrip() -> None:
    response = sequences_pb2.AppendResponse(IsSuccess=True, SequenceNumber=42)
    receipt = response.Receipt
    receipt.EventStore = "store"
    receipt.Namespace = "tenant"
    receipt.SequenceNumber = 42
    receipt.EventType.Id = "event"
    receipt.EventType.Generation = 1
    receipt.EventSourceId = "source"
    receipt.EventSourceType = "Account"
    receipt.EventStreamType = "Payments"
    receipt.EventStreamId = "period"
    receipt.Occurred.Value = "2020-01-01T00:00:00.1234567+00:00"
    receipt.Subject = "subject"
    receipt.Tags.append("tag")
    receipt.CausedBy.Subject = "caller"
    cause = receipt.Causation.add(Type="request")
    cause.Properties["source"] = "test"

    restored = sequences_pb2.AppendResponse.FromString(response.SerializeToString())

    assert restored.HasField("Receipt")
    assert restored.Receipt == receipt
    assert restored.Receipt.EventStreamId == "period"
    assert restored.Receipt.EventType.Id == "event"
    assert restored.Receipt.Causation[0].Properties["source"] == "test"


def test_batch_receipt_order_survives_a_roundtrip() -> None:
    response = sequences_pb2.AppendManyResponse(IsSuccess=True, SequenceNumbers=[41, 42])
    response.Receipts.add(SequenceNumber=41, EventSourceId="first")
    response.Receipts.add(SequenceNumber=42, EventSourceId="second")

    restored = sequences_pb2.AppendManyResponse.FromString(response.SerializeToString())

    assert [receipt.SequenceNumber for receipt in restored.Receipts] == list(restored.SequenceNumbers)
    assert [receipt.EventSourceId for receipt in restored.Receipts] == ["first", "second"]
