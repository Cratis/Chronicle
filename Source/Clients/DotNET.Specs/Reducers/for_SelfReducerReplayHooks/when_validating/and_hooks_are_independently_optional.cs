// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers.for_SelfReducerInvoker.given;

namespace Cratis.Chronicle.Reducers.for_SelfReducerReplayHooks.when_validating;

public class and_hooks_are_independently_optional : emitted_methods
{
    int[] _counts;

    void Because() => _counts = new[] { "BeginReplay", "EndReplay", "BeginReplayPartition", "EndReplayPartition" }.Select(name =>
    {
        var model = Emit(type =>
        {
            Method(type, typeof(Joined));
            var method = type.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, typeof(Task), name.EndsWith("Partition", StringComparison.Ordinal) ? [typeof(Partition)] : Type.EmptyTypes);
            method.GetILGenerator().Emit(OpCodes.Ldnull);
            method.GetILGenerator().Emit(OpCodes.Ret);
        });
        return new SelfReducerInvoker(Registry(typeof(Joined)), model, "state").ReplayHooks.Methods.Count();
    }).ToArray();

    [Fact] void should_require_no_companion_hooks() => _counts.ShouldEqual([1, 1, 1, 1]);
}
