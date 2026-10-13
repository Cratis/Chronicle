// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.when_validating_ownership;

public class and_a_classic_reducer_targets_the_self_model : given.self_reducers
{
    Type _classic;
    Exception _genericError;
    Exception _typeError;

    async Task Establish()
    {
        _classic = EmitClassicFor(typeof(PlainSelf));
        _options.ExplicitArtifacts.RegisterReducer<PlainSelf>();
        _store = CreateStore();
        await _store.DiscoverAll();
    }

    async Task Because()
    {
        _genericError = await Catch.Exception(RegisterClassic);
        _typeError = await Catch.Exception(() => _store.Reducers.Register(_classic));
    }

    async Task<IReducerHandler> RegisterClassic()
    {
        var method = _store.Reducers.GetType().GetMethods().Single(candidate => candidate.Name == nameof(IReducers.Register) && candidate.IsGenericMethod && candidate.GetGenericArguments().Length == 2);
        try
        {
            return await (Task<IReducerHandler>)method.MakeGenericMethod(_classic, typeof(PlainSelf)).Invoke(_store.Reducers, [])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    [Fact] void should_reject_generic_classic_registration() => _genericError.ShouldBeOfExactType<AmbiguousReadModelBuilder>();
    [Fact] void should_reject_type_classic_registration() => _typeError.ShouldBeOfExactType<AmbiguousReadModelBuilder>();
    [Fact] void should_keep_only_self_reduction() => _store.Reducers.GetAllHandlers().Select(handler => handler.ReducerType).ShouldContainOnly(typeof(PlainSelf));
}
