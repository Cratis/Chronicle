// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using Grpc.Core;
using ProtoBuf.Grpc;
using static Cratis.Chronicle.Services.given.hand_written_routing_surface;

namespace Cratis.Chronicle.Services.for_StoreEntitlementEnforcement;

public class when_every_hand_written_call_addresses_a_store_without_the_entitlement : given.a_store_entitlement_caller
{
    public static TheoryData<string> Routed()
    {
        var cases = new TheoryData<string>();
        foreach (var method in HandWritten.SelectMany(MethodsOf).Where(method => !KeyOf(method).StartsWith("ComplianceService.", StringComparison.Ordinal) && !Classification[KeyOf(method)].StartsWith(Exempt, StringComparison.Ordinal)))
        {
            cases.Add(KeyOf(method));
        }

        return cases;
    }

    [Theory]
    [MemberData(nameof(Routed))]
    public async Task should_refuse_the_call_when_enforcing(string key)
    {
        var error = await Invoke(key, ContextFor(true));
        (error as RpcException)?.StatusCode.ShouldEqual(StatusCode.PermissionDenied);
        (error is RpcException).ShouldBeTrue();
    }

    static async Task<Exception?> Invoke(string key, CallContext context)
    {
        var method = HandWritten.SelectMany(MethodsOf).Single(candidate => KeyOf(candidate) == key);
        var service = Create(method.DeclaringType);
        var arguments = method.GetParameters().Select(parameter =>
            parameter.ParameterType == typeof(CallContext) ? context : Request(parameter.ParameterType)).ToArray();
        try
        {
            var result = method.Invoke(service, arguments);
            if (result is Task task)
            {
                await task;
            }
            else if (result?.GetType().GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IObservable<>)) is { } observable)
            {
                await (Task)typeof(when_every_hand_written_call_addresses_a_store_without_the_entitlement)
                    .GetMethod(nameof(First), BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(observable.GetGenericArguments()[0]).Invoke(null, [result]);
            }

            return null;
        }
        catch (TargetInvocationException exception)
        {
            return exception.InnerException;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    static Task First<T>(IObservable<T> source) => source.FirstAsync().ToTask();

    static object Request(Type type)
    {
        var request = Activator.CreateInstance(type)!;
        foreach (var property in type.GetProperties().Where(property => property.PropertyType == typeof(string) && property.CanWrite))
        {
            property.SetValue(request, property.Name.Equals("EventStore", StringComparison.Ordinal) || property.Name.Equals("TargetEventStore", StringComparison.Ordinal) ? "shop" : "value");
        }

        return request;
    }
}
