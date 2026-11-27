// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces;

/// <summary>Converts lifecycle diagnostics outside the read model's generated query surface.</summary>
public static class NamespaceRemovalStatusConverters
{
    /// <summary>Converts an already authorized lifecycle snapshot without including internal failure details.</summary>
    /// <param name="state">The durable lifecycle.</param>
    /// <returns>The operator snapshot.</returns>
    public static NamespaceRemovalStatus ToStatus(NamespaceLifecycle state) => new(
        state.Namespace.Value,
        state.Status.ToString(),
        state.OperationId,
        state.Revision,
        state.Stage.ToString(),
        state.StageAttempts,
        state.TotalAttempts,
        state.BudgetStartAttempts,
        state.AttemptInProgress,
        state.DestructiveAttemptOutstanding,
        state.DestructiveActionIssued,
        state.LastFailure);
}
