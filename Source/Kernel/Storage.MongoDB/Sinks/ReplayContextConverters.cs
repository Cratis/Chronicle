// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Converters for <see cref="ReplayContext"/> to and from storage.
/// </summary>
public static class ReplayContextConverters
{
    /// <summary>
    /// Convert a <see cref="ReplayContext"/> to a <see cref="Chronicle.Storage.Sinks.ReplayContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> to convert.</param>
    /// <returns>The converted <see cref="Chronicle.Storage.Sinks.ReplayContext"/>.</returns>
    public static Chronicle.Storage.Sinks.ReplayContext ToChronicle(this ReplayContext context) => new(context.ModelName, context.RevertModelName, context.Started);

    /// <summary>
    /// Convert a <see cref="Chronicle.Storage.Sinks.ReplayContext"/> to a <see cref="ReplayContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="Chronicle.Storage.Sinks.ReplayContext"/> to convert.</param>
    /// <returns>The converted <see cref="ReplayContext"/>.</returns>
    public static ReplayContext ToStorage(this Chronicle.Storage.Sinks.ReplayContext context) => new(context.Model, context.RevertModel, context.Started);
}
