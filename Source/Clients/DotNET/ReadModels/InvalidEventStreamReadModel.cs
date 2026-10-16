// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a read model's event stream declaration is invalid.
/// </summary>
/// <param name="readModelType">The read model type.</param>
/// <param name="reason">Why the declaration is invalid.</param>
public sealed class InvalidEventStreamReadModel(Type readModelType, string reason)
    : Exception($"Read model '{readModelType.FullName}' has an invalid event stream declaration: {reason}");
