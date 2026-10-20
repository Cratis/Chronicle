// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;
using Cratis.Chronicle.Concepts.Security;
using Mongo = Cratis.Chronicle.Storage.MongoDB.ExternalServices.ExternalServiceDefinitionConverters;
using Sql = Cratis.Chronicle.Storage.Sql.EventStores.ExternalServices.ExternalServiceDefinitionConverters;

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthScope;

public class when_round_tripping_external_services : Specification
{
    ExternalServiceDefinition _sql;
    ExternalServiceDefinition _mongo;

    void Because()
    {
        var definition = new ExternalServiceDefinition("service", "Service", new ExternalServiceEndpoint(ExternalServiceEndpointType.Http, new HttpEndpointConfiguration("https://target.example", new OAuthAuthorization("https://authority.example", "client", "secret") { Scope = "resource/.default" }, new Dictionary<string, string>())));
        _sql = Sql.ToKernel(Sql.ToSql(definition));
        _mongo = Mongo.ToKernel(Mongo.ToMongoDB(definition));
    }

    [Fact] void should_preserve_scope_in_sql() => _sql.Endpoint.Http!.Authorization.AsT2.Scope.ShouldEqual("resource/.default");
    [Fact] void should_preserve_scope_in_mongodb() => _mongo.Endpoint.Http!.Authorization.AsT2.Scope.ShouldEqual("resource/.default");
}
