// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from './QueryResult';
import { IObservableQueryFor } from './IObservableQueryFor';
import { Constructor } from '@aksio/cratis-fundamentals';
import { useState, useEffect } from 'react';

/**
 * React hook for working with {@link IObservableQueryFor} within the state management of React.
 * @template TDataType Type of model the query is for.
 * @template TQuery Type of observable query to use.
 * @template TArguments Optional: Arguments for the query, if any
 * @param query Query type constructor.
 * @returns Tuple of {@link QueryResult} and a {@link PerformQuery} delegate.
 */
export function useObservableQuery<TDataType, TQuery extends IObservableQueryFor<TDataType>, TArguments = {}>(query: Constructor<TQuery>, args?: TArguments): [QueryResult<TDataType>] {
    const queryInstance = new query() as TQuery;
    const [result, setResult] = useState<QueryResult<TDataType>>(new QueryResult(queryInstance.defaultValue, true));

    useEffect(() => {
        if (queryInstance.requiresArguments && !args) {
            console.log(`Warning: Query '${query.name}' requires arguments. Will not perform the query.`);
            return;
        }

        const subscription = queryInstance.subscribe(_ => {
            setResult(_ as unknown as QueryResult<TDataType>);
        }, args);

        return () => subscription.unsubscribe();
    }, []);

    return [result];
}
