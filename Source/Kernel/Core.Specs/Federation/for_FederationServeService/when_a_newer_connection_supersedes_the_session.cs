// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeService;

public class when_a_newer_connection_supersedes_the_session : given.a_serve_service
{
    bool _firstEnded;

    async Task Establish()
    {
        await SetPolicy("acme", FederationDirection.AcceptorInbound);
        _presented = await Issued("acme");
    }

    async Task Because()
    {
        var older = Channel.CreateUnbounded<ServeBatch>();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var enumerator = _service.Serve(older.Reader.ReadAllAsync(cancellation.Token), cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        await enumerator.MoveNextAsync();
        await older.Writer.WriteAsync(Answer(enumerator.Current, "customer-store"), cancellation.Token);

        // A newer connection for the same subject, for instance through another silo.
        await _sessions["acme"].Open("newer");

        _firstEnded = !await enumerator.MoveNextAsync();
        await enumerator.DisposeAsync();
    }

    [Fact] void should_end_the_older_connection() => _firstEnded.ShouldBeTrue();
}
