// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Contracts.ExternalServices;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Storage.ExternalServices;
using Microsoft.Extensions.Logging.Abstractions;

using ContractEndpointType = Cratis.Chronicle.Contracts.ExternalServices.ExternalServiceEndpointType;
using ExternalServiceDefinitionContract = Cratis.Chronicle.Contracts.ExternalServices.ExternalServiceDefinition;

namespace Cratis.Chronicle.ExternalServices.for_ExternalService;

public class when_using_database_options : given.stored_headers
{
    readonly Dictionary<string, string> _options = new()
    {
        ["retained"] = "synthetic-retained",
        ["changed"] = "synthetic-before",
        ["omitted"] = "synthetic-omitted",
        ["whitespace"] = "synthetic-whitespace"
    };
    readonly Dictionary<string, string> _submitted = new()
    {
        ["retained"] = string.Empty,
        ["changed"] = "synthetic-after",
        ["new"] = string.Empty,
        ["whitespace"] = " "
    };
    readonly List<ExternalServiceResponse> _responses = [];
    readonly List<string> _wirePayloads = [];
    readonly List<Concepts.ExternalServices.ExternalServiceDefinition> _saved = [];
    IExternalServiceDefinitionsStorage _definitions;
    ExternalServiceDefinitionContract _contract;

    void Establish()
    {
        _definition = new("service", "service", new(Concepts.ExternalServices.ExternalServiceEndpointType.MsSql,
            Database: new("host", 1433, "database", "user", "synthetic-password", _options)));
        _definitions = _storage.GetEventStore("store").ExternalServices;
        _definitions.GetAll().Returns([_definition]);
        _definitions.Has("service").Returns(true);
        _definitions.Get("service").Returns(_definition);
        _definitions.Save(Arg.Any<Concepts.ExternalServices.ExternalServiceDefinition>()).Returns(call =>
        {
            _saved.Add(call.Arg<Concepts.ExternalServices.ExternalServiceDefinition>());
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        await QueryOverGrpc();
        _contract = _definition.ToContract();
        await UpdateSingle();
        await UpdateBatch("service");
        await UpdateBatch("new-service");
    }

    async Task QueryOverGrpc()
    {
        var service = new Services.ExternalServices.ExternalServices(Substitute.For<ICommandPipeline>(), _storage, NullLogger<Services.ExternalServices.ExternalServices>.Instance);
        var listed = await service.GetExternalServices(new() { EventStore = "store" });
        RoundTrip(listed.Data.Single());
        using var subscription = service.ObserveExternalServices(new() { EventStore = "store" }).Subscribe(result => RoundTrip(result.Data.Single()));
        _source.OnNext([_definition]);
        _options["changed"] = "synthetic-updated";
        _source.OnNext([_definition]);
    }

    void RoundTrip(ExternalServiceResponse response)
    {
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, response);
        _wirePayloads.Add(Encoding.UTF8.GetString(stream.ToArray()));
        stream.Position = 0;
        _responses.Add(ProtoBuf.Serializer.Deserialize<ExternalServiceResponse>(stream));
    }

    Task UpdateSingle() => new AddExternalService(
        "store",
        "service",
        "service",
        ContractEndpointType.MsSql,
        string.Empty,
        AuthorizationType.None,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        new Dictionary<string, string>(),
        "host",
        1433,
        "database",
        "user",
        "synthetic-password",
        _submitted).Handle(_storage);

    Task UpdateBatch(string id) => new AddExternalServices("store",
    [
        new()
        {
            Id = id,
            Name = "service",
            Endpoint = new()
            {
                Type = ContractEndpointType.MsSql,
                Database = new() { Host = "host", Port = 1433, Database = "database", Username = "user", Options = _submitted }
            }
        }
    ]).Handle(_storage);

    [Fact] void should_return_a_list_and_two_observations() => _responses.Count.ShouldEqual(3);
    [Fact] void should_keep_option_keys_in_every_response() => _responses.TrueForAll(_ => _.Options.Keys.Order().SequenceEqual(_options.Keys.Order())).ShouldBeTrue();
    [Fact] void should_blank_every_option_value() => _responses.SelectMany(_ => _.Options.Values).All(_ => _.Length == 0).ShouldBeTrue();
    [Fact] void should_not_serialize_option_values() => _wirePayloads.TrueForAll(_ => !_.Contains("synthetic-", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_blank_contract_option_values() => _contract.Endpoint.Database.Options.Values.All(_ => _.Length == 0).ShouldBeTrue();
    [Fact] void should_keep_contract_option_keys() => _contract.Endpoint.Database.Options.Keys.Order().SequenceEqual(_options.Keys.Order()).ShouldBeTrue();
    [Fact] void should_preserve_blank_values_in_both_update_paths() => _saved.Take(2).All(_ => _.Endpoint.Database!.Options["retained"] == "synthetic-retained").ShouldBeTrue();
    [Fact] void should_replace_non_blank_values_in_both_update_paths() => _saved.Take(2).All(_ => _.Endpoint.Database!.Options["changed"] == "synthetic-after").ShouldBeTrue();
    [Fact] void should_keep_whitespace_as_a_submitted_value() => _saved.Take(2).All(_ => _.Endpoint.Database!.Options["whitespace"] == " ").ShouldBeTrue();
    [Fact] void should_drop_omitted_keys_in_both_update_paths() => _saved.Take(2).All(_ => !_.Endpoint.Database!.Options.ContainsKey("omitted")).ShouldBeTrue();
    [Fact] void should_keep_new_blank_keys_blank() => _saved.TrueForAll(_ => _.Endpoint.Database!.Options["new"].Length == 0).ShouldBeTrue();
    [Fact] void should_not_restore_a_value_for_a_new_service() => _saved[2].Endpoint.Database!.Options["retained"].ShouldEqual(string.Empty);
    [Fact] void should_not_mutate_stored_option_values() => _definition.Endpoint.Database!.Options["retained"].ShouldEqual("synthetic-retained");
    [Fact] void should_not_mutate_submitted_option_values() => _submitted["retained"].ShouldEqual(string.Empty);
}
