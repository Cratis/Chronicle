// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    }

    /**
     * Create a {@link QueryResult} from a {@link Response}.
     * @template TModel Type of model to create for.
     * @param {Response} [response] Response to create from.
     * @returns A new {@link QueryResult}.
     */
    static async fromResponse<TModel>(response: Response): Promise<QueryResult<TModel>> {
        const jsonResponse = await response.json() as QueryResultFromServer<TModel>;
        return new QueryResult(jsonResponse.data, jsonResponse.isSuccess && response.ok);
    }
}
