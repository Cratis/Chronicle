// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_getting_snapshots_by_id;

public class and_it_is_a_reducer_with_compliance_data : given.all_dependencies
{
    class MyReadModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    ReadModelKey _key;
    IEnumerable<ReadModelSnapshot<MyReadModel>> _result = [];
    IReducerHandler _handler = null!;
    ICompliance _compliance = null!;

    void Establish()
    {
        _key = "test-id";

        _projections.HasFor<MyReadModel>().Returns(false);
        _reducers.HasReducerFor(typeof(MyReadModel)).Returns(true);

        _handler = Substitute.For<IReducerHandler>();
        _handler.EventSequenceId.Returns(new EventSequenceId("custom-sequence"));
        _reducers.GetHandlerForReadModelType(typeof(MyReadModel)).Returns(_handler);

        _services.ReadModels.GetSnapshotsByKey(Arg.Any<GetSnapshotsByKeyRequest>()).Returns(new GetSnapshotsByKeyResponse
        {
            Snapshots =
            [
                new()
                {
                    ReadModel = """{"Id":"test-id","Name":"Original Name"}""",
                    Events = [],
                    Occurred = DateTimeOffset.UtcNow,
                    CorrelationId = Guid.NewGuid()
                }
            ]
        });

        var schema = new JsonSchema();
        schema.ExtensionData[ComplianceJsonSchemaExtensions.ComplianceKey] = new List<ComplianceSchemaMetadata> { new("pii", "{}") };
        _schemaGenerator.Generate(Arg.Any<Type>()).Returns(schema);

        _compliance = Substitute.For<ICompliance>();
        _services.Compliance.Returns(_compliance);
        _compliance.Release(Arg.Any<ReleaseRequest>()).Returns(new ReleaseResponse
        {
            Payload = """{"Id":"test-id","Name":"Released Name"}"""
        });
    }

    async Task Because() => _result = await _readModels.GetSnapshotsById<MyReadModel>(_key);

    [Fact] void should_call_compliance_release() => _compliance.Received(1).Release(Arg.Any<ReleaseRequest>());
    [Fact] void should_return_snapshot_with_released_instance() => _result.First().Instance.Name.ShouldEqual("Released Name");
    [Fact] void should_preserve_snapshot_metadata() => _result.First().Instance.Id.ShouldEqual("test-id");
}
