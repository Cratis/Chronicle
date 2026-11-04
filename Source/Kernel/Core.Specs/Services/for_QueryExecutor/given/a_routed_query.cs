// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.for_QueryExecutor.given;

/// <summary>
/// Base context for a query whose routing values are checked before the query runs.
/// </summary>
public class a_routed_query : global::Cratis.Chronicle.Security.given.a_system_store_caller
{
    /// <summary>
    /// Counts what happened while a query was routed.
    /// </summary>
    public class Probe
    {
        /// <summary>Gets the number of times the query ran.</summary>
        public int Invocations { get; private set; }

        /// <summary>Gets the number of times the System store access check was resolved.</summary>
        public int AccessResolutions { get; private set; }

        /// <summary>Records that the query ran.</summary>
        /// <returns>A result.</returns>
        public Task<IEnumerable<int>> Run()
        {
            Invocations++;
            return Task.FromResult<IEnumerable<int>>([1]);
        }

        /// <summary>Records that the observable query ran.</summary>
        /// <returns>A stream.</returns>
        public IObservable<IEnumerable<int>> Observe()
        {
            Invocations++;
            return Observable.Return<IEnumerable<int>>([1]);
        }

        /// <summary>Records that the access check was resolved.</summary>
        public void Resolved() => AccessResolutions++;
    }

    protected CallContext CallFor(Probe probe, ClaimsPrincipal? principal, bool served = true)
    {
        _caller = principal;
        var services = CreateServices();
        var counted = new ServiceCollection()
            .AddSingleton<global::Cratis.Chronicle.Security.ISystemStoreAccess>(_ =>
            {
                probe.Resolved();
                return services.GetRequiredService<global::Cratis.Chronicle.Security.ISystemStoreAccess>();
            })
            .BuildServiceProvider();
        var http = new DefaultHttpContext { User = principal ?? new ClaimsPrincipal(), RequestServices = counted };
        return new CallContext(new object(), new FakeServerCallContext(served ? http : null));
    }
}
