// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the request for registering projections.
/// </summary>
[ProtoContract]
public class RegisterRequest
{
    /// <summary>
    /// Gets or sets the <see cref="ProjectionAndPipeline"/> instances to register.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IList<ProjectionAndPipeline> ProjectionsAndPipelines { get; set; } = [];
}
