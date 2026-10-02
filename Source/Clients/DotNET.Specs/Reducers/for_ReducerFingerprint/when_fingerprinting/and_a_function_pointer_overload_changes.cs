// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_a_function_pointer_overload_changes : Specification
{
    const string Members = "public unsafe ReadModel Reduce(Event @event, ReadModel current) => new(Helper.Apply((delegate* unmanaged[Cdecl]<int, int>)0));";
    const string Helper = """
        public static unsafe class Helper
        {
            public static int Apply(delegate* unmanaged[Cdecl]<int, int> callback) => 1;
            public static int Apply(delegate* unmanaged[Stdcall]<int, int> callback) => 2;
        }
        """;
    string _cdecl;
    string _stdcall;

    void Because()
    {
        _cdecl = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members, before: Helper));
        _stdcall = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members.Replace("[Cdecl]", "[Stdcall]"), before: Helper));
    }

    [Fact] void should_detect_retargeting_to_a_different_calling_convention_overload() => _cdecl.ShouldNotEqual(_stdcall);
}
