// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider;

/// <summary>
/// The authentication interceptor reads the token synchronously on whatever thread makes the call - including the
/// keep-alive answer that keeps the session alive. When a fresh token is cached that read must never queue behind
/// another caller: an await that does not complete synchronously posts its continuation to the calling
/// <see cref="SynchronizationContext"/>, which is blocked waiting for that very result.
/// </summary>
public class when_a_fresh_token_is_requested_synchronously_while_another_caller_holds_the_provider : Specification
{
    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    OAuthTokenProvider _provider;
    TokenCacheEndpointHandler _handler;
    ChronicleServerAddress _address = new("server-a.example");
    ManualResetEventSlim _holderInsideProvider;
    ManualResetEventSlim _releaseHolder;
    int _holderThreadId;
    Task _holder;
    Task<string?> _synchronousCaller;
    string? _token;
    bool _completed;

    async Task Establish()
    {
        _holderInsideProvider = new(false);
        _releaseHolder = new(false);
        _handler = new TokenCacheEndpointHandler { AccessToken = "token-a" };
        _provider = new OAuthTokenProvider(
            () =>
            {
                if (Environment.CurrentManagedThreadId == Volatile.Read(ref _holderThreadId))
                {
                    _holderInsideProvider.Set();
                    _releaseHolder.Wait(_timeout);
                }

                return _address;
            },
            "test-client",
            "test-secret",
            NullLogger<OAuthTokenProvider>.Instance,
            _handler,
            TimeProvider.System);

        await _provider.GetAccessToken();
    }

    void Because()
    {
        _holder = Task.Factory.StartNew(
            () =>
            {
                Volatile.Write(ref _holderThreadId, Environment.CurrentManagedThreadId);
                return _provider.GetAccessToken();
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default).Unwrap();
        _holderInsideProvider.Wait(_timeout);

        _synchronousCaller = Task.Factory.StartNew(
            () =>
            {
                // Like a call made on an Orleans grain turn or any other single-threaded scheduler: continuations
                // are posted back to this thread, which is blocked on the result.
                SynchronizationContext.SetSynchronizationContext(new QueueingSynchronizationContext());
                return _provider.GetAccessToken().GetAwaiter().GetResult();
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        Thread.Sleep(300);
        _releaseHolder.Set();
        _completed = _synchronousCaller.Wait(_timeout);
        _token = _completed ? _synchronousCaller.Result : null;
    }

    void Destroy()
    {
        _releaseHolder.Set();
        _holder.Wait(_timeout);
        _provider.Dispose();
    }

    [Fact] void should_complete_without_waiting_for_the_other_caller() => _completed.ShouldBeTrue();
    [Fact] void should_return_the_cached_token() => _token.ShouldEqual("token-a");
    [Fact] void should_not_fetch_a_new_token() => _handler.TokenRequests.ShouldEqual(1);

    sealed class QueueingSynchronizationContext : SynchronizationContext
    {
        readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = [];

        public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));
    }
}
