// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Marks a host in which the kernel services are called directly, without a network request. A command that
/// arrives without a caller in such a host comes from the host itself.
/// </summary>
[IgnoreConvention]
sealed class InProcessKernelHost;
