// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Security.Claims;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventStores;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader.given;

[Collection("SystemStoreGuard")]
public class an_http_statistics_reader : Services.ReadModels.for_ReadModels.given.all_dependencies
{
    protected StatisticsReader _reader;
    protected HttpContext _httpContext;
    protected IAdministratorPrivileges _privileges;
    protected INamespaces _namespaces;
    protected ISink _secondSink;
    protected UserId _administratorId;
    IServiceProvider? _previousServices;
    ServiceProvider _services;
    IHttpContextAccessor _accessor;
    HttpContext? _previousHttpContext;

    protected virtual Authentication AuthenticationOptions => new();

    protected virtual bool HasAmbientRequest => true;

    protected void BeginRequest() => _accessor.HttpContext = HasAmbientRequest ? _httpContext : null;

    void Establish()
    {
        _previousServices = SystemStoreGuard.InProcessServices;
        _accessor = new HttpContextAccessor();
        _previousHttpContext = _accessor.HttpContext;
        _administratorId = Guid.NewGuid();
        _privileges = Substitute.For<IAdministratorPrivileges>();
        _privileges.IsActiveAdministrator(_administratorId).Returns(true);
        var options = Options.Create(new ChronicleOptions { Authentication = AuthenticationOptions });
        var decisions = new AuthorizationDecisions(options, NullLogger<AuthorizationDecisions>.Instance, httpContextAccessor: _accessor);
        _services = new ServiceCollection()
            .AddSingleton(_accessor)
            .AddSingleton<IAdministratorAuthorization>(new AdministratorAuthorization(options, _privileges, NullLogger<AdministratorAuthorization>.Instance))
            .AddSingleton<ISystemStoreAccess>(new SystemStoreAccessVerdict(decisions))
            .AddSingleton<IAuthorizationDecisions>(decisions)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .BuildServiceProvider();
        _httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", _administratorId.ToString()), new Claim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User)], "spec")),
            RequestServices = _services
        };
        _accessor.HttpContext = _httpContext;
        SystemStoreGuard.InProcessServices = _services;
        ConfigureStatistics();
    }

    void ConfigureStatistics()
    {
        _namespaces = Substitute.For<INamespaces>();
        _namespaces.GetAll().Returns(["first", "second"]);
        _grainFactory.GetGrain<INamespaces>("System").Returns(_namespaces);
        _secondSink = Substitute.For<ISink>();
        _readBindings.Resolve(Arg.Any<EventStoreName>(), (EventStoreNamespaceName)"second", Arg.Any<ReadModelDefinition>(), Arg.Any<ReadModelContainerName?>())
            .Returns(call => new ReadModelReadBinding(_secondSink, call.ArgAt<ReadModelContainerName?>(3)) { Lease = SinkProvenanceLease.Exempt });
        _sink.GetInstances(Arg.Any<Concepts.ReadModels.ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new ReadModelInstances([Row("first", 3)], 1));
        _secondSink.GetInstances(Arg.Any<Concepts.ReadModels.ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new ReadModelInstances([Row("second", 5)], 1));
        _reader = new(_service, _grainFactory, Substitute.For<IEventStoreInitialization>());
    }

    void Destroy()
    {
        _accessor.HttpContext = _previousHttpContext;
        SystemStoreGuard.InProcessServices = _previousServices;
        _services.Dispose();
    }

    static ExpandoObject Row(string @namespace, long count)
    {
        IDictionary<string, object?> row = new ExpandoObject();
        row["eventType"] = "shared";
        row["namespace"] = @namespace;
        row["count"] = count;
        return (ExpandoObject)row;
    }
}
