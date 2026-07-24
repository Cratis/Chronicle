// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Json;

namespace Cratis.Infrastructure;

/// <summary>
/// Assembly-level initialization for the specs.
/// </summary>
internal static class ModuleInitialization
{
    /// <summary>
    /// Pre-warms the global <see cref="Globals.JsonSerializerOptions"/> on a single thread before any
    /// tests run. Its lazy initializer publishes the options instance before it has finished adding the
    /// derived-type converter, so two test classes racing the first access under xUnit's parallel
    /// execution can freeze it mid-configuration — surfacing as an intermittent
    /// "JsonSerializerOptions instance is read-only" failure. Touching it here makes the first (and
    /// only) initialization single-threaded, matching the kernel's startup pre-warm.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize() => _ = Globals.JsonSerializerOptions;
}
