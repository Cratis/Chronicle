// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModels;

#pragma warning disable CA2263
public class when_getting_instances_and_it_is_a_reducer_with_compliance_data : given.all_dependencies
{
    class MyReadModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    IEnumerable<MyReadModel> _result = [];
    ICompliance _compliance = null!;

    void Establish()
    {
        _projections.HasFor(typeof(MyReadModel)).Returns(false);
        _reducers.HasFor(typeof(MyReadModel)).Returns(true);
        _reducers.GetInstances(typeof(MyReadModel), null).Returns(
        [
            new MyReadModel { Id = "id-1", Name = "First" },
            new MyReadModel { Id = "id-2", Name = "Second" }
        ]);

        var schema = new JsonSchema();
        schema.ExtensionData[ComplianceJsonSchemaExtensions.ComplianceKey] = new List<ComplianceSchemaMetadata> { new("pii", "{}") };
        _schemaGenerator.Generate(Arg.Any<Type>()).Returns(schema);

        _compliance = Substitute.For<ICompliance>();
        _services.Compliance.Returns(_compliance);
        _compliance.Release(Arg.Is<ReleaseRequest>(r => r.Subject == "id-1")).Returns(new ReleaseResponse { Payload = """{"Id":"id-1","Name":"Released First"}""" });
        _compliance.Release(Arg.Is<ReleaseRequest>(r => r.Subject == "id-2")).Returns(new ReleaseResponse { Payload = """{"Id":"id-2","Name":"Released Second"}""" });
    }

    async Task Because() => _result = await _readModels.GetInstances<MyReadModel>();

    [Fact] void should_call_compliance_release_for_each_instance() => _compliance.Received(2).Release(Arg.Any<ReleaseRequest>());
    [Fact] void should_return_all_released_instances() => _result.Count().ShouldEqual(2);
    [Fact] void should_return_first_released_instance() => _result.First().Name.ShouldEqual("Released First");
    [Fact] void should_return_second_released_instance() => _result.Last().Name.ShouldEqual("Released Second");
}
#pragma warning restore CA2263
