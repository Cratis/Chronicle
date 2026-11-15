// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>The private client connection settings for the workload. Never serialize or log.</summary>
/// <param name="Endpoint">The mapped client endpoint.</param>
/// <param name="ClientId">The client identity.</param>
/// <param name="ClientSecret">The ephemeral client credential.</param>
internal sealed record WorkloadConnection(string Endpoint, string ClientId, string ClientSecret);
