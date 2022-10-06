// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IQueryFor } from './IQueryFor';
import { Constructor } from '@aksio/cratis-fundamentals';
import { useState, useEffect } from 'react';
import { QueryResultWithState } from './QueryResultWithState';

/**
 * Delegate type for performing a {@link IQueryFor} in the context of the {@link useQuery} hook.
 */
export type PerformQuery<TArguments = {}> = (args?: TArguments) => Promise<void>;

/**
 * React hook for working with {@link IQueryFor} within the state management of React.
 * @template TDataType Type of model the query is for.
 * @template TQuery Type of query to use.
 * @template TArguments Optional: Arguments for the query, if any
 * @param query Query type constructor.
 * @returns Tuple of {@link QueryResult} and a {@link PerformQuery} delegate.
 */
export function useQuery<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = {}>(query: Constructor<TQuery>, args?: TArguments): [QueryResultWithState<TDataType>, PerformQuery<TArguments>] {
    const queryInstance = new query() as TQuery;
    const [result, setResult] = useState<QueryResultWithState<TDataType>>(new QueryResultWithState(queryInstance.defaultValue, true, true, false));
    const queryExecutor = (async (args?: TArguments) => {
        const response = await queryInstance.perform(args as any);
        setResult(new QueryResultWithState(response.data, response.isSuccess, false, response.isSuccess));
    });

    useEffect(() => {
        queryExecutor(args);
    }, []);

    return [result, queryExecutor];
}
