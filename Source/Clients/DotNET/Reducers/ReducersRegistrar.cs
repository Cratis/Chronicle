// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducersRegistrar"/>.
/// </summary>
public class ReducersRegistrar : IReducersRegistrar
{
    /// <inheritdoc/>
    public Task Initialize() => throw new NotImplementedException();

    /// <inheritdoc/>
    public IReducerHandler GetById(ReducerId id) => throw new NotImplementedException();
}
