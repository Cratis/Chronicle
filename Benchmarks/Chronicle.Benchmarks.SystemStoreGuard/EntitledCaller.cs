// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Orleans;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Benchmarks.SystemStoreGuard;

/// <summary>
/// An application caller of the event store "bench" whose grant is, or is not, in a published per-silo snapshot.
/// </summary>
sealed class EntitledCaller : IDisposable
{
    readonly StoreEntitlementsCache _cache;

    public EntitledCaller(bool entitled, bool enforce)
    {
        var holder = new PrincipalKey(PrincipalKey.LocalIssuer, "app");
        var grants = new List<EventStoreAccessGrant>();
        if (entitled) grants.Add(new(holder, "bench", null, StoreAccess.Read | StoreAccess.Append | StoreAccess.Manage, EventStoreAccessGrantSource.Command));
        _cache = new(Substitute.For<IGrainFactory>(), NullLogger<StoreEntitlementsCache>.Instance, TimeProvider.System);
        _cache.Publish(new(1, grants));
        var options = Options.Create(new ChronicleOptions { Authentication = new Authentication { Enabled = true, EnforceStoreEntitlements = enforce } });
        var accessor = new HttpContextAccessor();
        Services = new ServiceCollection()
            .AddSingleton<IHttpContextAccessor>(accessor)
            .AddSingleton<IAuthorizationDecisions>(new AuthorizationDecisions(new SystemStoreAuthorizationProbe(false), options, NullLogger<AuthorizationDecisions>.Instance, _cache))
            .AddSingleton<IStoreEntitlementsCache>(_cache)
            .AddSingleton(options)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .BuildServiceProvider();
        User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "app")], "Bearer"));
        Http = new DefaultHttpContext { User = User, RequestServices = Services };
        accessor.HttpContext = Http;
        Call = new CallContext(new object(), new BenchmarkServerCallContext(Http));
    }

    public IServiceProvider Services { get; }

    public ClaimsPrincipal User { get; }

    public HttpContext Http { get; }

    public CallContext Call { get; }

    public void Dispose() => _cache.Dispose();

    sealed class BenchmarkServerCallContext(HttpContext http) : ServerCallContext
    {
        readonly Dictionary<object, object> _userState = new() { ["__HttpContext"] = http };

        protected override string MethodCore => "method";

        protected override string HostCore => "host";

        protected override string PeerCore => "peer";

        protected override DateTime DeadlineCore => DateTime.MaxValue;

        protected override Metadata RequestHeadersCore => [];

        protected override CancellationToken CancellationTokenCore => default;

        protected override Metadata ResponseTrailersCore => [];

        protected override Status StatusCore { get; set; }

        protected override WriteOptions? WriteOptionsCore { get; set; }

        protected override AuthContext AuthContextCore => new(null, new Dictionary<string, List<AuthProperty>>());

        protected override IDictionary<object, object> UserStateCore => _userState;

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}
