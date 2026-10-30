// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Represents the materialization bindings not supported.
/// </summary>
public class MaterializationBindingsNotSupported() : NotSupportedException("This storage provider does not support materialization bindings.");
