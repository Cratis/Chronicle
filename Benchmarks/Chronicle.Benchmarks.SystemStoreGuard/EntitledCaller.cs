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
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Benchmarks.SystemStoreGuard;

/// <summary>
/// An application caller of the event store "bench" whose grant is, or is not, in a published per-silo snapshot.
/// </summary>
sealed class EntitledCaller : IDisposable
{
    /// <summary>The number of independent requests in each measured batch.</summary>
    public const int RequestCount = 16_384;

    readonly StoreEntitlementsCache _cache;
    readonly HttpContextAccessor _accessor;
    HttpContext[] _requests = [];
    CallContext[] _calls = [];
    int _next;

    /// <summary>Initializes an isolated caller and entitlement snapshot.</summary>
    /// <param name="entitled">Whether the caller has a grant.</param>
    /// <param name="enforce">Whether to enforce grants.</param>
    public EntitledCaller(bool entitled, bool enforce)
    {
        var holder = new PrincipalKey(PrincipalKey.LocalIssuer, "app");
        var grants = new List<EventStoreAccessGrant>();
        if (entitled) grants.Add(new(holder, "bench", null, StoreAccess.Read | StoreAccess.Append | StoreAccess.Manage, EventStoreAccessGrantSource.Command));
        _cache = new(Substitute.For<IGrainFactory>(), NullLogger<StoreEntitlementsCache>.Instance, TimeProvider.System);
        _cache.Publish(new(1, grants));
        var options = Options.Create(new ChronicleOptions { Authentication = new Authentication { Enabled = true, EnforceStoreEntitlements = enforce } });
        var accessor = _accessor = new HttpContextAccessor();
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

    /// <summary>Gets the request services.</summary>
    public IServiceProvider Services { get; }

    /// <summary>Gets the current caller.</summary>
    public ClaimsPrincipal User { get; private set; }

    /// <summary>Gets the current request.</summary>
    public HttpContext Http { get; private set; }

    /// <summary>Gets the current RPC context.</summary>
    public CallContext Call { get; private set; }

    /// <summary>Prepares one fresh request per measured invocation.</summary>
    public void PrepareRequests()
    {
        _next = 0;
        _requests = new HttpContext[RequestCount];
        _calls = new CallContext[RequestCount];
        for (var index = 0; index < _requests.Length; index++)
        {
            _requests[index] = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "app")], "Bearer")),
                RequestServices = Services
            };
            _calls[index] = new CallContext(new object(), new BenchmarkServerCallContext(_requests[index]));
        }
    }

    /// <summary>Selects the next prepared request.</summary>
    /// <returns>The fresh RPC context.</returns>
    public CallContext NextRequest()
    {
        Http = _requests[_next];
        User = Http.User;
        _accessor.HttpContext = Http;
        Call = _calls[_next++];
        return Call;
    }

    /// <inheritdoc/>
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
