// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDefinitionComparer.when_comparing;

public class with_only_the_managed_identity_client_id_changed : Specification
{
    WebhookDefinitionComparer _comparer;
    WebhookDefinition _definition;
    WebhookDefinitionComparisonResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Webhooks.Has("webhook").Returns(true);
        _comparer = new(storage, Substitute.For<IObjectComparer>(), Substitute.For<ILogger<WebhookDefinitionComparer>>());
        _definition = new("webhook", WebhookOwner.Client, "log", [], new("https://example.com/webhook", new ManagedIdentityAuthorization("client", "api://resource/.default"), new Dictionary<string, string>()));
    }

    async Task Because() => _result = await _comparer.Compare(new("webhook", "store"), _definition, _definition with { Target = _definition.Target with { Authorization = _definition.Target.Authorization.AsT4 with { ClientId = "other-client" } } });

    [Fact] void should_detect_the_change() => _result.Result.ShouldEqual(WebhookDefinitionCompareResult.Different);
    [Fact] void should_identify_the_authorization_change() => _result.ChangedProperties!.AuthorizationChanged.ShouldBeTrue();
    [Fact] void should_leave_the_pins_unchanged() => _result.ChangedProperties!.EventTypesChanged.ShouldBeFalse();
}
