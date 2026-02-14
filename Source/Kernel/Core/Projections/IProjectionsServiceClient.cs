// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Services;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Defines a client for <see cref="IProjectionsService"/>.
/// </summary>
public interface IProjectionsServiceClient : IGrainServiceClient<IProjectionsService>, IProjectionsService;
