// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Defines a system that decides whether a <see cref="WhenClause"/> matches an observed <see cref="CaptureChange"/>.
/// </summary>
public interface IWhenClauseEvaluator
{
    /// <summary>
    /// Decide whether the given <see cref="WhenClause"/> matches the given <see cref="CaptureChange"/>.
    /// </summary>
    /// <param name="when">The <see cref="WhenClause"/> to evaluate.</param>
    /// <param name="change">The <see cref="CaptureChange"/> to evaluate against.</param>
    /// <returns>True when the clause matches, false when not.</returns>
    bool Matches(WhenClause when, CaptureChange change);
}
