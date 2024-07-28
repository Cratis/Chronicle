// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationResult } from '../validation/ValidationResult';

/**
 * Defines the result from executing a query.
 */
export interface IQueryResult<TDataType> {
    /**
     * Gets the data result from the query.
     */
    readonly data: TDataType;

    /**
     * Gets whether or not the query executed successfully.
     */
    readonly isSuccess: boolean;

    /**
     * Gets whether or not the query was authorized to execute.
     */
    readonly isAuthorized: boolean;

    /**
     * Gets whether or not the query is valid.
     */
    readonly isValid: boolean;

    /**
     * Gets whether or not there are any exceptions that occurred.
     */
    readonly hasExceptions: boolean;

    /**
     * Gets any validation errors. If this collection is empty, there are errors.
     */
    readonly validationResults: ValidationResult[];

    /**
     * Gets any exception messages that might have occurred.
     */
    readonly exceptionMessages: string[];

    /**
     * Gets the stack trace if there was an exception.
     */
    readonly exceptionStackTrace: string;

    /**
     * Gets whether or not the query has data.
     */
    readonly hasData: boolean;
}
