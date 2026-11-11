// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Represents the parent found for a future, along with the child key it is applied with.
/// </summary>
/// <param name="ParentIndexers">The <see cref="ArrayIndexer"/> chain from the root to the parent.</param>
/// <param name="RetriedKey">The <see cref="ResolvedKey"/> from retrying a deferred key, if the parent was found that way.</param>
/// <param name="ChildKey">The converted key of the child.</param>
internal sealed record ResolvedParent(List<ArrayIndexer> ParentIndexers, ResolvedKey? RetriedKey, object? ChildKey);
