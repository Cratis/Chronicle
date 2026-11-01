// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;

namespace Cratis.Chronicle.Benchmarks.ClientStartup;

/// <summary>
/// Emits non-generic self-reducing models, as required by structural reducer discovery.
/// </summary>
public static class CatalogSelfModel
{
    /// <summary>
    /// Creates a model with a static Reduce(event, current) returning a fresh model.
    /// </summary>
    /// <param name="module">The catalog module.</param>
    /// <param name="eventType">The handled event type.</param>
    /// <param name="index">The distinct model index.</param>
    /// <returns>The completed model type.</returns>
    public static Type Create(ModuleBuilder module, Type eventType, int index)
    {
        var builder = module.DefineType($"CatalogSelfModel{index}", TypeAttributes.Public | TypeAttributes.Sealed);
        var constructor = builder.DefineDefaultConstructor(MethodAttributes.Public);
        var method = builder.DefineMethod("Reduce", MethodAttributes.Public | MethodAttributes.Static, builder, [eventType, builder]);
        var body = method.GetILGenerator();
        body.Emit(OpCodes.Newobj, constructor);
        body.Emit(OpCodes.Ret);
        return builder.CreateType();
    }
}
