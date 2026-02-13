// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Defines a service for registering custom BSON serializers.
/// </summary>
public interface ICustomSerializers
{
    /// <summary>
    /// Registers all custom BSON serializers.
    /// </summary>
    void Register();
}
