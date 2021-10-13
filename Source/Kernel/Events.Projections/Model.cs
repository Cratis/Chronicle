// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Schema;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents the model used by a projection to project to.
    /// </summary>
    /// <param name="Name">Name of the model.</param>
    /// <param name="Schema">The <see cref="JSchema"/> for the model.</param>
    public record Model(string Name, JSchema Schema);
}
