// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using Cratis.Chronicle.Security.given;
using Grpc.Core;
using ProtoBuf.Grpc;
using static Cratis.Chronicle.Services.given.hand_written_routing_surface;

namespace Cratis.Chronicle.Services;

public class when_hand_written_services_receive_system_requests : for_QueryExecutor.given.a_routed_query
{
    public static TheoryData<string> Routed(string classification)
    {
        var cases = new TheoryData<string>();
        foreach (var method in HandWritten.SelectMany(MethodsOf).Where(method => Classification[KeyOf(method)] == classification))
        {
            cases.Add(KeyOf(method));
        }

        return cases;
    }

    public static TheoryData<string> Admins() => Routed(Admin);

    public static TheoryData<string> Refusals() => Routed(Refused);

    [Theory]
    [MemberData(nameof(Admins))]
    public async Task should_refuse_a_caller_who_is_not_an_administrator(string key)
    {
        foreach (var name in new[] { a_system_store_caller.RevokedAdministrator, a_system_store_caller.OrdinaryUser, a_system_store_caller.Application, a_system_store_caller.Anonymous })
        {
            var error = await Invoke(key, CallFor(new Probe(), CallerNamed(name)));
            Refusal(error).ShouldEqual(StatusCode.PermissionDenied);
        }
    }

    [Theory]
    [MemberData(nameof(Refusals))]
    public async Task should_refuse_every_caller_including_an_administrator(string key)
    {
        foreach (var name in new[] { a_system_store_caller.Administrator, a_system_store_caller.OrdinaryUser, a_system_store_caller.Anonymous })
        {
            var error = await Invoke(key, CallFor(new Probe(), CallerNamed(name)));
            Refusal(error).ShouldEqual(StatusCode.PermissionDenied);
        }
    }

    [Theory]
    [MemberData(nameof(Admins))]
    public async Task should_refuse_a_value_with_the_separator_before_asking_who_is_calling(string key)
    {
        var probe = new Probe();
        var error = await Invoke(key, CallFor(probe, CallerNamed(a_system_store_caller.Administrator)), "a#b");

        Refusal(error).ShouldEqual(StatusCode.InvalidArgument);
        probe.AccessResolutions.ShouldEqual(0);
    }

    static StatusCode Refusal(Exception? error) => error is RpcException rpc ? rpc.StatusCode : StatusCode.OK;

    static async Task<Exception?> Invoke(string key, CallContext context, string store = "System")
    {
        var method = HandWritten.SelectMany(MethodsOf).Single(candidate => KeyOf(candidate) == key);
        var service = Create(method.DeclaringType!);
        var arguments = method.GetParameters().Select(parameter =>
            parameter.ParameterType == typeof(CallContext) ? context : Request(parameter.ParameterType, store)).ToArray();
        try
        {
            var result = method.Invoke(service, arguments);
            switch (result)
            {
                case Task task:
                    await task;
                    break;
                default:
                    if (result?.GetType().GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IObservable<>)) is { } observable)
                    {
                        await (Task)typeof(when_hand_written_services_receive_system_requests)
                            .GetMethod(nameof(First), BindingFlags.NonPublic | BindingFlags.Static)!
                            .MakeGenericMethod(observable.GetGenericArguments()[0]).Invoke(null, [result])!;
                    }

                    break;
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

    static object Request(Type type, string store)
    {
        var request = Activator.CreateInstance(type)!;
        foreach (var property in type.GetProperties().Where(property => property.PropertyType == typeof(string) && property.CanWrite))
        {
            property.SetValue(request, property.Name.Equals("EventStore", StringComparison.Ordinal) || property.Name.Equals("TargetEventStore", StringComparison.Ordinal) ? store : "value");
        }

        return request;
    }
}
