// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator.given;

public class a_recovery_operation : a_coordinator
{
    protected NamespaceLifecycle _original;

    void Establish()
    {
        var recovering = NamespaceLifecycle.Open(_name, new NamespaceIncarnationId(Guid.NewGuid()), _clock.GetUtcNow())
            .BeginRemoval(Guid.NewGuid(), _clock.GetUtcNow())
            .RequireRecovery("NamespaceRemovalBudgetExhausted");
        _state = recovering with { TotalAttempts = NamespaceLifecycle.MaximumAttempts };
        _original = _state;
        _markerState = new(_state.Incarnation, false, 1);
    }
}
