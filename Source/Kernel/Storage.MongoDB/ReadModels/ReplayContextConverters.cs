// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.ReadModels;

/// <summary>
/// Converters for <see cref="ReplayContext"/> to and from storage.
/// </summary>
public static class ReplayContextConverters
{
    /// <summary>
    /// Convert a <see cref="ReplayContext"/> to a <see cref="Chronicle.Storage.ReadModels.ReplayContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> to convert.</param>
    /// <returns>The converted <see cref="Chronicle.Storage.ReadModels.ReplayContext"/>.</returns>
    public static Chronicle.Storage.ReadModels.ReplayContext ToChronicle(this ReplayContext context) =>
        new(new(context.ReadModel, context.Generation), context.ReadModelName, context.RevertReadModelName, context.Started);

    /// <summary>
    /// Convert a <see cref="Chronicle.Storage.ReadModels.ReplayContext"/> to a <see cref="ReplayContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="Chronicle.Storage.ReadModels.ReplayContext"/> to convert.</param>
    /// <returns>The converted <see cref="ReplayContext"/>.</returns>
    public static ReplayContext ToMongoDB(this Chronicle.Storage.ReadModels.ReplayContext context) =>
        new(context.Type.Identifier, context.Type.Generation, context.ReadModel, context.RevertReadModel, context.Started);
}
