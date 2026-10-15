// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a read model has no observable collection.
/// </summary>
/// <param name="readModelType">The read model type.</param>
/// <param name="reason">The reason it cannot be observed.</param>
public class ReadModelCannotBeObserved(Type readModelType, string reason)
    : Exception($"Read model '{readModelType.FullName}' cannot be observed: {reason}.");
