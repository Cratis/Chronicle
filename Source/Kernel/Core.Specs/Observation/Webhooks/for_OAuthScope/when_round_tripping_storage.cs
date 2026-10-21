// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;
using Mongo = Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks.WebhookDefinitionConverters;
using Sql = Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.WebhookDefinitionConverters;

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthScope;

public class when_round_tripping_storage : Specification
{
    WebhookDefinition _sql;
    WebhookDefinition _mongo;
    WebhookDefinition _contract;

    void Because()
    {
        var definition = new WebhookDefinition("webhook", WebhookOwner.Client, "log", [], new("https://target.example", new OAuthAuthorization("https://authority.example", "client", "secret") { Scope = "resource/.default" }, new Dictionary<string, string>()));
        _sql = Sql.ToKernel(Sql.ToSql(definition));
        _mongo = Mongo.ToKernel(Mongo.ToMongoDB(definition));
        _contract = definition.ToContract().ToChronicle();
    }

    [Fact] void should_preserve_scope_in_sql() => _sql.Target.Authorization.AsT2.Scope.ShouldEqual("resource/.default");
    [Fact] void should_preserve_scope_in_mongodb() => _mongo.Target.Authorization.AsT2.Scope.ShouldEqual("resource/.default");
    [Fact] void should_preserve_scope_on_the_wire() => _contract.Target.Authorization.AsT2.Scope.ShouldEqual("resource/.default");
}
