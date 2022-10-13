// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from './QueryResult';

/**
 * Represents a specialized {@link QueryResult<TDataType} that holds state for its execution
 */

export class QueryResultWithState<TDataType> extends QueryResult<TDataType> {

    /**
     * Initializes an instance of {@link QueryResultWithState<TDataType>}.
     * @param {TDataType} data The items returned, if any - can be empty.
     * @param {boolean} isSuccess Whether or not the query was successful.
     * @param {boolean} isPerforming Whether or not the query is being performed. True if its performing, false if it is done.
     */
    constructor(readonly data: TDataType, readonly isSuccess: boolean, readonly isPerforming: boolean) {
        super(data, isSuccess);
    }
}
