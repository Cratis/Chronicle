// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Json;

namespace Cratis.Chronicle;

/// <summary>
/// Assembly-level initialization for the specs.
/// </summary>
internal static class ModuleInitialization
{
    /// <summary>
    /// Pre-warms the global <see cref="Globals.JsonSerializerOptions"/> on a single thread before any tests run.
    /// </summary>
    /// <remarks>
    /// Its lazy initializer assigns the static field before it has finished adding the derived-type converter, so a
    /// second thread reading the property in that window gets a half-configured instance and serializing with it
    /// freezes it - the first thread's remaining <c>Converters.Add</c> then throws "JsonSerializerOptions instance is
    /// read-only". Under xUnit's parallel execution that made any spec touching the options intermittently fail,
    /// whichever one happened to lose the race. Touching it here makes the first, and only, initialization
    /// single-threaded, the same way the kernel pre-warms it during startup configuration.
    /// </remarks>
    [ModuleInitializer]
    internal static void Initialize() => _ = Globals.JsonSerializerOptions;
}
