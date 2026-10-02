// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.when_fingerprinting;

public class and_a_function_pointer_field_changes : Specification
{
    const string Members = "public unsafe ReadModel Reduce(Event @event, ReadModel current) => new((int)(nint)Helper.Callback);";
    const string Helper = "public static unsafe class Helper { public static delegate* unmanaged[Cdecl]<int, int> Callback; }";
    string _cdecl;
    string _stdcall;

    void Because()
    {
        _cdecl = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members, before: Helper));
        _stdcall = ReducerFingerprint.Create(given.CompiledReducer.Compile(Members, before: Helper.Replace("[Cdecl]", "[Stdcall]")));
    }

    [Fact] void should_detect_a_changed_field_calling_convention() => _cdecl.ShouldNotEqual(_stdcall);
}
