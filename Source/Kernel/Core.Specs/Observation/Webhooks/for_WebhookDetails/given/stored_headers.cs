// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDetails.given;

public class stored_headers : Specification
{
    protected readonly Dictionary<string, string> _headers = new()
    {
        ["x-first"] = "synthetic-first",
        ["x-second"] = "synthetic-second",
        ["x-empty"] = string.Empty,
        ["x-whitespace"] = " "
    };
    protected readonly Subject<IEnumerable<WebhookDefinition>> _source = new();
    protected IStorage _storage;
    protected WebhookDefinition _definition;

    void Establish()
    {
        _definition = new(
            "webhook",
            WebhookOwner.Client,
            EventSequenceId.Log,
            [],
            new("https://example.com", new BearerTokenAuthorization("synthetic-authorization"), _headers));
        _storage = Substitute.For<IStorage>();
        var definitions = Substitute.For<IWebhookDefinitionsStorage>();
        _storage.GetEventStore("store").Webhooks.Returns(definitions);
        definitions.GetAll().Returns([_definition]);
        definitions.ObserveAll().Returns(_source);
    }

    void Destroy() => _source.Dispose();
}
