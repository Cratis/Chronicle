// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Definitions
{
    /// <summary>
    /// Represents the definition of a model used by a projection.
    /// </summary>
    /// <param name="Name">Name of the model.</param>
    /// <param name="Schema">The JSON schema for the model.</param>
    public record ModelDefinition(string Name, string Schema);
}
