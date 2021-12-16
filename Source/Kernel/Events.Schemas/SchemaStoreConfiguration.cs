// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Schemas
{
    /// <summary>
    /// Represents the configuration for <see cref="ISchemaStore"/>.
    /// </summary>
    /// <param name="Host">MongoDB host address.</param>
    /// <param name="Port">MongoDB port.</param>
    public record SchemaStoreConfiguration(string Host, int Port);
}
