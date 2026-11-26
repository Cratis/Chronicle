// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.given;

/// <summary>
/// A call that was made to a grain.
/// </summary>
/// <param name="InterfaceName">The grain interface that was called.</param>
/// <param name="MethodName">The method that was called.</param>
/// <param name="Target">The identity of the grain that was called.</param>
public record RecordedGrainCall(string InterfaceName, string MethodName, string Target);
