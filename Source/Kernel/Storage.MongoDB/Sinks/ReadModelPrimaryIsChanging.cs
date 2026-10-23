// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// The exception that is thrown when a primary lookup cannot distinguish an absent key from a replay promotion.
/// </summary>
/// <param name="container">The primary container being promoted.</param>
public class ReadModelPrimaryIsChanging(ReadModelContainerName container)
    : Exception($"The primary read model container '{container}' changed during the lookup; retry the read.");
