// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// The exception that is thrown when a read model names a sink that is not configured in the kernel.
/// </summary>
/// <param name="readModel">The <see cref="ReadModelIdentifier"/> of the read model.</param>
/// <param name="name">The <see cref="SinkName"/> it names.</param>
public class UnknownSinkName(ReadModelIdentifier readModel, SinkName name)
    : Exception($"Read model '{readModel}' stores to sink '{name}', which is not configured. Add it to the kernel's 'sinks.configurations', or remove [Sink(\"{name}\")] from the read model.");
