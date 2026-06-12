// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModels;

#pragma warning disable CA2263
public class when_getting_instance_by_id_and_it_is_a_reducer_with_compliance_data : given.all_dependencies
{
    class MyReadModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    ReadModelKey _key;
    MyReadModel _result = null!;
    ICompliance _compliance = null!;

    void Establish()
    {
        _key = "test-id";

        _projections.HasFor(typeof(MyReadModel)).Returns(false);
        _reducers.HasFor(typeof(MyReadModel)).Returns(true);
        _reducers.GetInstanceById(typeof(MyReadModel), _key).Returns(new MyReadModel { Id = "test-id", Name = "Original Name" });

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

    async Task Because() => _result = await _readModels.GetInstanceById<MyReadModel>(_key);

    [Fact] void should_call_compliance_release() => _compliance.Received(1).Release(Arg.Any<ReleaseRequest>());
    [Fact] void should_return_released_instance() => _result.Name.ShouldEqual("Released Name");
}
#pragma warning restore CA2263
