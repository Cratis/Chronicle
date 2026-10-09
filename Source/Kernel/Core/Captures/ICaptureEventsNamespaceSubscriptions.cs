// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Defines the grain that subscribes events captures when a namespace is added. Keyed by the event store name.
/// </summary>
public interface ICaptureEventsNamespaceSubscriptions : IGrainWithStringKey;
