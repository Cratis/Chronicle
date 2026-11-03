// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649, MA0048, SA1402
namespace Foreign.Driver;

public class PoolExhaustedException(string message) : Exception(message);
