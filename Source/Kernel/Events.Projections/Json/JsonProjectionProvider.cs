// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Json
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionProvider"/> for JSON based definitions.
    /// </summary>
    public class JsonProjectionProvider : IProjectionProvider
    {
        /// <inheritdoc/>
        public IProjection All => throw new NotImplementedException();
    }
}
