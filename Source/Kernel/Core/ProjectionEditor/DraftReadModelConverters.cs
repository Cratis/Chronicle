// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Converts between the generated contract representation of a draft read model and the one the commands take.
/// </summary>
public static class DraftReadModelConverters
{
    /// <summary>
    /// Converts a contract <see cref="Contracts.ProjectionEditor.DraftReadModel"/> to a <see cref="DraftReadModel"/>.
    /// </summary>
    /// <param name="draft">The contract draft read model.</param>
    /// <returns>The draft read model the commands take.</returns>
    public static DraftReadModel ToApi(this Contracts.ProjectionEditor.DraftReadModel draft) =>
        new(draft.Identifier, draft.DisplayName, draft.ContainerName, draft.Schema);
}
