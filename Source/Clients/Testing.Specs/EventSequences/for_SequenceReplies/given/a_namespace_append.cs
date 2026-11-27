// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Sequences;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Testing.EventSequences.for_SequenceReplies.given;

public class a_namespace_append : Specification
{
    protected readonly CancellationTokenSource _lifetime = new();
    protected readonly Guid _identity = Guid.NewGuid();
    protected IEventSequences _inner = null!;
    protected DropReplyInterceptor _replies = null!;
    private protected SequenceReplies _subject = null!;
    protected AppendOnceToNamespaceRequest _request = null!;
    protected CommandResult<AppendOnceResponse> _response = null!;
    protected CommandResult<AppendOnceResponse> _actual = null!;
    protected CallContext _context;
    protected Exception _error = null!;

    void Establish()
    {
        _inner = Substitute.For<IEventSequences>();
        _replies = new();
        _subject = new(_inner, _replies);
        _context = new(new CallOptions(cancellationToken: _lifetime.Token));
        _request = new() { EventStore = "store", Namespace = "tenant", ExpectedIncarnation = _identity, IdempotencyKey = "key" };
        _response = CommandResult<AppendOnceResponse>.Success(Guid.NewGuid(), new()
        {
            IsSuccess = true,
            SequenceNumber = 42,
            ConstraintViolations = [],
            Errors = []
        });
        _inner.AppendOnceToNamespace(_request, _context).Returns(_response);
    }

    void Destroy() => _lifetime.Dispose();
}
