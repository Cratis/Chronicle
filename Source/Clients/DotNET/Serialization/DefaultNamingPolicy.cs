// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serialization;

/// <summary>
/// Default implementation of <see cref="INamingPolicy"/>.
/// </summary>
public class DefaultNamingPolicy : INamingPolicy
{
    /// <inheritdoc/>
    public string GetReadModelName(Type readModel) => readModel.Name;

    /// <inheritdoc/>
    public string GetPropertyName(string name) => name;
}
