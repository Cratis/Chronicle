// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.EventSequences.for_NamespaceBoundAppendStorage.given;

public class a_failing_provider : a_bound_storage
{
    protected readonly List<Exception> _observed = [];

    protected void SetFailure(Exception failure)
    {
        _provider.Append(_context, _event).ThrowsAsync(failure);
        _provider.AppendMany(_context, _events).ThrowsAsync(failure);
        _provider.AppendManyWithIdentities(_context, _events).ThrowsAsync(failure);
        _provider.TryGetPublication(_context, _publication).ThrowsAsync(failure);
        _provider.TryGetPublications(_context, _identities).ThrowsAsync(failure);
        _provider.AppendPublication(_context, _publication, _event).ThrowsAsync(failure);
        _provider.GetEffectReceipt(_context, _receipt.Id).ThrowsAsync(failure);
        _provider.GetEffectReceipts(_context, _occurrence).ThrowsAsync(failure);
        _provider.GetEffectReceiptsForOccurrences(_context, _occurrences).ThrowsAsync(failure);
        _provider.AppendManyWithEffectReceipt(_context, _events, _receipt).ThrowsAsync(failure);
        _provider.ClearReceivedCalls();
    }

    protected async Task ObserveFailures()
    {
        foreach (var operation in Operations()) _observed.Add(await Cratis.Specifications.Catch.Exception(operation));
    }
}
