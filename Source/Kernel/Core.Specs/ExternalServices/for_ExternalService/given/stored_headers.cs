// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ExternalServices;

namespace Cratis.Chronicle.ExternalServices.for_ExternalService.given;

public class stored_headers : Specification
{
    protected readonly Dictionary<string, string> _headers = new()
    {
        ["x-first"] = "synthetic-first",
        ["x-second"] = "synthetic-second",
        ["x-empty"] = string.Empty,
        ["x-whitespace"] = " "
    };
    protected readonly Subject<IEnumerable<ExternalServiceDefinition>> _source = new();
    protected IStorage _storage;
    protected ExternalServiceDefinition _definition;

    void Establish()
    {
        _definition = new("service", "service", new(ExternalServiceEndpointType.Http,
            new("https://example.com", new BearerTokenAuthorization("synthetic-authorization"), _headers)));
        _storage = Substitute.For<IStorage>();
        var definitions = Substitute.For<IExternalServiceDefinitionsStorage>();
        _storage.GetEventStore("store").ExternalServices.Returns(definitions);
        definitions.GetAll().Returns([_definition]);
        definitions.ObserveAll().Returns(_source);
    }

    void Destroy() => _source.Dispose();
}
