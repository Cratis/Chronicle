// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.given;

public class near_misses : self_reducers
{
    protected Type _nearMiss;
    protected ILogger<Reducers> _logger;

    void Establish()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("NearMiss").DefineType("NearMissModel", TypeAttributes.Public);
        var method = type.DefineMethod("Reduce", MethodAttributes.Public | MethodAttributes.Static, type, [typeof(OwnershipEvent), type, typeof(string)]);
        method.GetILGenerator().Emit(OpCodes.Ldnull);
        method.GetILGenerator().Emit(OpCodes.Ret);
        _nearMiss = type.CreateType();
        _logger = Substitute.For<ILogger<Reducers>>();
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(typeof(Reducers).FullName!).Returns(_logger);
    }

    protected bool WarnedAboutNearMiss() => _logger.ReceivedCalls().Any(call =>
        call.GetMethodInfo().Name == nameof(ILogger.Log) && call.GetArguments()[0] is LogLevel.Warning &&
        call.GetArguments()[2]!.ToString()!.Contains(_nearMiss.FullName!));
}
