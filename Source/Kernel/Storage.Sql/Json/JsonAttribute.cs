// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Json;

/// <summary>
/// Attribute that marks a property as being stored in a Json column.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class JsonAttribute : Attribute;
