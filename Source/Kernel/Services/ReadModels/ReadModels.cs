// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModels"/>.
/// </summary>
internal class ReadModels : IReadModels
{
    /// <inheritdoc/>
    public Task Register(RegisterRequest request, CallContext context = default) => throw new NotImplementedException();
}
