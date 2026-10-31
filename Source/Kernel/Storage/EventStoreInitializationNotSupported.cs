// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage;

/// <summary>
/// The exception that is thrown when a custom storage provider does not support initialization markers.
/// </summary>
public class EventStoreInitializationNotSupported() : Exception("The storage provider must implement durable event store initialization markers.");
