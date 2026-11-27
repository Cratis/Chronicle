// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions;

public partial class UnitOfWork : INamespaceBoundUnitOfWork
{
    NamespaceBinding? _namespaceBinding;

    /// <inheritdoc/>
    public void BindNamespace(NamespaceBinding binding)
    {
        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            ThrowIfUnitOfWorkIsCompleted();
            if (_completing) throw new DecisionReadAfterCompletion();
            binding.RequireFor(_eventStore.Value.Name, _eventStore.Value.Namespace);
            if (_namespaceBinding == binding) return;
            if (_namespaceBinding is not null || _eventSequence is not null) throw new NamespaceBindingNotValid();
            if (_hasEnrolledDecisionReads || _stagedEvents.Count > 0) throw new NamespaceBindingNotValid();
            _namespaceBinding = binding;
        }
    }

    IEventSequence BindNamespaceWriter(IEventSequence writer)
    {
        if (_namespaceBinding is null) return writer;
        return writer is EventSequence native
            ? native.BindNamespace(_namespaceBinding)
            : throw new NamespaceBindingsNotSupported();
    }
}
