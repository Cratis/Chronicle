// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_ClientRegistration;

/// <summary>
/// The read model of the projection registered while the kernel is busy.
/// </summary>
/// <param name="Number">The number of the last event projected.</param>
public record HeldProjectionReadModel(int Number);
