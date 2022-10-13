// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@aksio/cratis-fundamentals';
import { JsonSerializer } from '@aksio/cratis-fundamentals/JsonSerializer';

type QueryResultFromServer<TDataType> = {
    data: TDataType;
    isSuccess: boolean;
};

/**
 * Represents the result from executing a {@link IQueryFor}.
 * @template TDataType The data type.
 */
export class QueryResult<TDataType> {
    /**
     * Creates an instance of query result.
     * @param {TDataType} data The items returned, if any - can be empty.
     * @param {boolean} isSuccess Whether or not the query was successful.
     */
    constructor(readonly data: TDataType, readonly isSuccess: boolean) {
        console.log('hello');
    }

    /**
     * Create a {@link QueryResult} from a {@link Response}.
     * @template TModel Type of model to create for.
     * @param {Response} [response] Response to create from.
     * @returns A new {@link QueryResult}.
     */
    static async fromResponse<TModel>(response: Response, instanceType: Constructor, enumerable: boolean): Promise<QueryResult<TModel>> {
        const jsonResponse = await response.json() as QueryResultFromServer<TModel>;

        let data: any = jsonResponse.data;
        if (enumerable) {
            data = JsonSerializer.deserializeArrayFromInstance(instanceType, data);
        } else {
            data = JsonSerializer.deserializeFromInstance(instanceType, data);
        }

        return new QueryResult(data, jsonResponse.isSuccess && response.ok);
    }

    /**
     * Gets whether or not the query has data.
     */
    get hasData(): boolean {
        if (this.data) {
            if (this.data.constructor === Array) {
                if ((this.data as any).length || 0 > 0) {
                    return true;
                }
            } else {
                return true;
            }
        }

        return false;
    }
}
