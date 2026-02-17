// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Serialization;

/// <summary>
/// Disables auto registration of bson serializer.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class BsonSerializerDisableAutoRegistrationAttribute : Attribute;
