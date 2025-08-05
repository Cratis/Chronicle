// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Models;
using Cratis.Serialization;
using Cratis.Strings;

namespace Cratis.Chronicle.Serialization;

/// <summary>
/// Default implementation of <see cref="INamingPolicy"/>.
/// </summary>
/// <param name="modelNameResolver">The <see cref="IModelNameResolver"/> to use for resolving model names.</param>
public class CamelCaseNamingPolicy(IModelNameResolver modelNameResolver) : INamingPolicy
{
    /// <inheritdoc/>
    public JsonNamingPolicy JsonPropertyNamingPolicy => AcronymFriendlyJsonCamelCaseNamingPolicy.Instance;

    /// <inheritdoc/>
    public string GetReadModelName(Type readModel) => modelNameResolver.GetNameFor(readModel);

    /// <inheritdoc/>
    public string GetPropertyName(string name) => name.ToCamelCase();
}
