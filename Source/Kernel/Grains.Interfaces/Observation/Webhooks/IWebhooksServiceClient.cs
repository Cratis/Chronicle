// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Services;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Defines a client for <see cref="IWebhooksService"/>.
/// </summary>
public interface IWebhooksServiceClient : IGrainServiceClient<IWebhooksService>, IWebhooksService;
