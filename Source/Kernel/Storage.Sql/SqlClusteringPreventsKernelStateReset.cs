// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The exception that is thrown when a kernel state reset would erase live SQL cluster membership.
/// </summary>
public sealed class SqlClusteringPreventsKernelStateReset()
    : Exception("Kernel state reset is not supported with Clustering.Type 'Sql': it would erase membership for the running cluster. Use an isolated Localhost cluster for development resets.");
