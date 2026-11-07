// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Services.for_QueryExecutor;

/// <summary>
/// A <see cref="ServerCallContext"/> that carries a request the way the ASP.NET Core gRPC host does.
/// </summary>
public class FakeServerCallContext : ServerCallContext
{
    readonly Dictionary<object, object> _userState = [];
    readonly CancellationToken _cancellationToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeServerCallContext"/> class.
    /// </summary>
    /// <param name="httpContext">The request, or null for a call that is not served over HTTP.</param>
    /// <param name="cancellationToken">The optional token that ends the call.</param>
    public FakeServerCallContext(HttpContext? httpContext, CancellationToken cancellationToken = default)
    {
        _cancellationToken = cancellationToken;
        if (httpContext is not null)
        {
            _userState["__HttpContext"] = httpContext;
        }
    }

    /// <inheritdoc/>
    protected override string MethodCore => "method";

    /// <inheritdoc/>
    protected override string HostCore => "host";

    /// <inheritdoc/>
    protected override string PeerCore => "peer";

    /// <inheritdoc/>
    protected override DateTime DeadlineCore => DateTime.MaxValue;

    /// <inheritdoc/>
    protected override Metadata RequestHeadersCore => [];

    /// <inheritdoc/>
    protected override CancellationToken CancellationTokenCore => _cancellationToken;

    /// <inheritdoc/>
    protected override Metadata ResponseTrailersCore => [];

    /// <inheritdoc/>
    protected override Status StatusCore { get; set; }

    /// <inheritdoc/>
    protected override WriteOptions? WriteOptionsCore { get; set; }

    /// <inheritdoc/>
    protected override AuthContext AuthContextCore => new(null, new Dictionary<string, List<AuthProperty>>());

    /// <inheritdoc/>
    protected override IDictionary<object, object> UserStateCore => _userState;

    /// <inheritdoc/>
    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();

    /// <inheritdoc/>
    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
}
