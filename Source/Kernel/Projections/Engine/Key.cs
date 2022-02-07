// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Represents the key typically coming from a <see cref="KeyResolver"/>.
    /// </summary>
    /// <param name="Value">The actual key value.</param>
    /// <param name="ArrayIndexers">Any array indexers.</param>
    public record Key(object Value, IEnumerable<ArrayIndexer> ArrayIndexers);
}
