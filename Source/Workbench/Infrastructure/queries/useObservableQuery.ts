// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState } from './QueryResultWithState';
import { IObservableQueryFor } from './IObservableQueryFor';
import { Constructor } from 'Infrastructure';
import { useState, useEffect } from 'react';
import { QueryResult } from './QueryResult';

/**
 * React hook for working with {@link IObservableQueryFor} within the state management of React.
 * @template TDataType Type of model the query is for.
 * @template TQuery Type of observable query to use.
 * @template TArguments Optional: Arguments for the query, if any
 * @param query Query type constructor.
 * @returns Tuple of {@link QueryResult} and a {@link PerformQuery} delegate.
 */
export function useObservableQuery<TDataType, TQuery extends IObservableQueryFor<TDataType>, TArguments = {}>(query: Constructor<TQuery>, args?: TArguments): [QueryResultWithState<TDataType>] {
    const queryInstance = new query() as TQuery;
    const [result, setResult] = useState<QueryResultWithState<TDataType>>(QueryResultWithState.empty(queryInstance.defaultValue));
    const argumentsDependency = queryInstance.requestArguments.map(_ => args?.[_]);

    useEffect(() => {
        const subscription = queryInstance.subscribe(response => {
            setResult(QueryResultWithState.fromQueryResult(response, false));
        }, args as any);

        return () => subscription.unsubscribe();
    }, argumentsDependency);

    return [result];
}
