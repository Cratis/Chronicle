// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Defines a reducer for a specific model.
/// </summary>
/// <typeparam name="TModel">Type of model the reducer is for.</typeparam>
public interface IReducerFor<TModel> : IReducer
    where TModel : class;
