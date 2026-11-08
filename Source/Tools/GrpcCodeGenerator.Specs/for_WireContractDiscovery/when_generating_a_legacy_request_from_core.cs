// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_WireContractDiscovery;

[Collection(SharedTypeRegistryCollection.Name)]
public class when_generating_a_legacy_request_from_core : Specification
{
    string _outputDir;
    string _generated;
    string _file;
    IReadOnlyList<Type> _discovered;
    IReadOnlyDictionary<string, int> _tags;

    void Establish()
    {
        SharedTypeRegistry.Configure(2, "Cratis.Chronicle.Contracts");
        _outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _file = Path.Combine(_outputDir, "Compliance", "ErasureWireRequest.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(_file)!);
        const string existing = """
            using ProtoBuf;
            namespace Cratis.Chronicle.Contracts.Compliance;
            [ProtoContract] public class ErasureWireRequest {
                [ProtoMember(1)] public string EventStore { get; set; }
                [ProtoMember(2)] public string Namespace { get; set; }
                [ProtoMember(3)] public string Identifier { get; set; }
                [ProtoMember(4)] public string? RequestId { get; set; }
            }
            """;
        File.WriteAllText(_file, existing);
    }

    void Because()
    {
        _discovered = new WireContractDiscovery(typeof(ErasureWireRequest).Assembly).Discover();
        SharedTypeRegistry.QualifiedNameFor(typeof(ErasureWireRequest));
        _generated = new ServiceInterfaceGenerator(2, "Cratis.Chronicle.Contracts").GenerateSharedType(typeof(ErasureWireRequest), _outputDir);
        _tags = ProtoMemberIndexReader.ReadExistingIndexes(_file, nameof(ErasureWireRequest));
    }

    [Fact] void should_discover_the_explicit_core_artifact() => _discovered.ShouldContain(typeof(ErasureWireRequest));
    [Fact] void should_leave_unmarked_models_out() => _discovered.ShouldNotContain(typeof(when_generating_a_legacy_request_from_core));
    [Fact] void should_keep_the_existing_event_store_tag() => _tags["EventStore"].ShouldEqual(1);
    [Fact] void should_keep_the_existing_namespace_tag() => _tags["Namespace"].ShouldEqual(2);
    [Fact] void should_keep_the_existing_identifier_tag() => _tags["Identifier"].ShouldEqual(3);
    [Fact] void should_keep_the_retry_identity_tag() => _tags["RequestId"].ShouldEqual(4);
    [Fact] void should_keep_the_identity_optional() => _generated.ShouldContain("string? RequestId");
    [Fact] void should_not_replace_the_legacy_service_interface() => File.Exists(Path.Combine(_outputDir, "Compliance", "ICompliance.cs")).ShouldBeFalse();

    void Destroy() => Directory.Delete(_outputDir, recursive: true);
}
