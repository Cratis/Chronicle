// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>The machine a run was made on.</summary>
/// <param name="OperatingSystem">The operating system description.</param>
/// <param name="Architecture">The process architecture.</param>
/// <param name="CpuCount">The logical CPU count.</param>
/// <param name="MemoryBytes">The memory available to the process.</param>
/// <param name="DockerMemoryBytes">The memory the Docker engine has, or <see langword="null"/> when Docker is not used.</param>
/// <param name="DockerCpus">The CPUs the Docker engine has, or <see langword="null"/>.</param>
public sealed record HardwareDescription(string OperatingSystem, string Architecture, int CpuCount, long MemoryBytes, long? DockerMemoryBytes, int? DockerCpus);
