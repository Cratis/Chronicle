// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Registrations;

/// <summary>
/// The exception thrown when a storage provider does not support durable registration intents. The kernel then applies
/// registrations before it answers, as it did before it accepted them.
/// </summary>
public class RegistrationIntentsStorageNotSupported() : Exception("The storage provider does not support durable registration intents.");
