// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// The forward-only lifecycle shared by inventory and successor obligations.
/// </summary>
public enum ObserverDefinitionChangeRowState
{
    /// <summary>
    /// Work is intended, or a nonterminal progress checkpoint is recorded.
    /// </summary>
    Intended = 0,

    /// <summary>
    /// The work's Start has answered. A late answer cannot replace a terminal row.
    /// </summary>
    Started = 1,

    /// <summary>
    /// The outcome is durable, or the successor obligation has been settled.
    /// </summary>
    Terminal = 2
}
