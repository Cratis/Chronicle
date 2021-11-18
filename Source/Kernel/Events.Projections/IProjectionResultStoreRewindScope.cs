// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines the scope when doing rewind.
    /// </summary>
    public interface IProjectionResultStoreRewindScope : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="Model"/> the scope is for.
        /// </summary>
        Model Model { get; }
    }
}
