// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the response for getting all read model definitions.
/// </summary>
[ProtoContract]
public class GetDefinitionsResponse
{
    /// <summary>
    /// Gets or sets the collection of <see cref="ReadModelDefinition"/> instances.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IList<ReadModelDefinition> ReadModels { get; set; } = [];
}
