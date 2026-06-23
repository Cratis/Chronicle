// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelReactorInvoker"/>.
/// </summary>
/// <param name="sideEffectHandlers">The <see cref="IReactorSideEffectHandlers"/> used to append returned events as side effects.</param>
/// <param name="contextValuesBuilder">The <see cref="IReactorContextValuesBuilder"/> used to resolve append-metadata for side-effect events.</param>
/// <param name="logger">The <see cref="ILogger{T}"/> for logging.</param>
public class ReadModelReactorInvoker(
    IReactorSideEffectHandlers sideEffectHandlers,
    IReactorContextValuesBuilder contextValuesBuilder,
    ILogger<ReadModelReactorInvoker> logger) : IReadModelReactorInvoker
{
    /// <inheritdoc/>
    public async Task Invoke(
        IEventStore eventStore,
        object reactor,
        ReadModelReactorMethod method,
        object? readModel,
        EventContext changeContext,
        IServiceProvider serviceProvider)
    {
        var arguments = BuildArguments(method, readModel, changeContext, serviceProvider);
        var returnValue = method.Method.Invoke(reactor, arguments);
        await HandleReturnValue(eventStore, reactor, method.Method, returnValue, changeContext);
    }

    static object?[] BuildArguments(ReadModelReactorMethod method, object? readModel, EventContext changeContext, IServiceProvider serviceProvider)
    {
        var parameters = method.Method.GetParameters();
        var arguments = new object?[parameters.Length];
        arguments[0] = method.IsCollection ? ToCollection(method.ReadModelType, readModel) : readModel;

        for (var index = 1; index < parameters.Length; index++)
        {
            var parameterType = parameters[index].ParameterType;
            arguments[index] = parameterType == typeof(EventContext)
                ? changeContext
                : serviceProvider.GetRequiredService(parameterType);
        }

        return arguments;
    }

    static Array ToCollection(Type readModelType, object? readModel)
    {
        var array = Array.CreateInstance(readModelType, readModel is null ? 0 : 1);
        if (readModel is not null)
        {
            array.SetValue(readModel, 0);
        }

        return array;
    }

    async Task HandleReturnValue(IEventStore eventStore, object reactor, MethodInfo method, object? returnValue, EventContext changeContext)
    {
        if (method.ReturnType == typeof(void))
        {
            return;
        }

        if (returnValue is Task task)
        {
            await task;

            if (!method.ReturnType.IsGenericType)
            {
                return;
            }

            var result = task.GetType().GetProperty(nameof(Task<object>.Result))?.GetValue(task);
            if (result is not null)
            {
                await HandleSideEffect(eventStore, reactor, result, changeContext);
            }

            return;
        }

        if (returnValue is not null)
        {
            await HandleSideEffect(eventStore, reactor, returnValue, changeContext);
        }
    }

    async Task HandleSideEffect(IEventStore eventStore, object reactor, object result, EventContext changeContext)
    {
        var context = new ReactorContext(changeContext, reactor, contextValuesBuilder.Build(reactor, changeContext));
        if (!sideEffectHandlers.CanHandle(context, result))
        {
            logger.ReadModelReactorReturnValueNotHandled(result.GetType().Name);
            return;
        }

        var handleResult = await sideEffectHandlers.Handle(context, eventStore, result);
        if (!handleResult.IsSuccess && handleResult.TryGetError(out var failure) && failure is not null)
        {
            logger.ReadModelReactorSideEffectAppendFailed(string.Join(Environment.NewLine, failure.GetMessages()));
        }
    }
}
