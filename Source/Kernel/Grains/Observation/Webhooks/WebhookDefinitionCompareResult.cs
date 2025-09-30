// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents the result of comparing two <see cref="ProjectionDefinition">projection definitions</see>.
/// </summary>
public enum WebhookDefinitionCompareResult
{
    /// <summary>
    /// The definitions are the same.
    /// </summary>
    Same = 0,

    /// <summary>
    /// The definitions is new.
    /// </summary>
    New = 1,

    /// <summary>
    /// The definitions are different.
    /// </summary>
    Different = 2
}
