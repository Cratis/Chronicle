// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Configuration
{
    /// <summary>
    /// Represents the configuration for storage.
    /// </summary>
    /// <param name="EventStore">The <see cref="EventStore"/> configuration object.</param>
    public record Storage(EventStore EventStore);
}
