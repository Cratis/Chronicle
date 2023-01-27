// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor, JsonSerializer } from '@aksio/cratis-fundamentals';
import { ValidationResult } from '../validation/ValidationResult';
import { IQueryResult } from './IQueryResult';

type QueryResultFromServer<TDataType> = {
    data: TDataType;
    isSuccess: boolean;
};

/**
 * Represents the result from executing a {@link IQueryFor}.
 * @template TDataType The data type.
 */
export class QueryResult<TDataType = {}> implements IQueryResult<TDataType> {

    static empty<TDataType>(defaultValue: TDataType): QueryResult<TDataType> {
        return new QueryResult({
            data: defaultValue,
            isSuccess: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
        }, Object, false);
    }

    static noSuccess: QueryResult = new QueryResult({
        data: {},
        isSuccess: false,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
    }, Object, false);

    /**
     * Creates an instance of query result.
     * @param {*} result The raw result from the backend.
     * @param {Constructor} instanceType The type of instance to deserialize.
     * @param {boolean} enumerable Whether or not the result is supposed be an enumerable or not.
     */
    constructor(result: any, instanceType: Constructor, enumerable: boolean) {
        this.isSuccess = result.isSuccess;
        this.isAuthorized = result.isAuthorized;
        this.isValid = result.isValid;
        this.hasExceptions = result.hasExceptions;
        this.validationResults = result.validationResults.map(_ => new ValidationResult(_.severity, _.message, _.members, _.state));
        this.exceptionMessages = result.exceptionMessages;
        this.exceptionStackTrace = result.exceptionStackTrace;

        if (result.data) {
            let data: any = result.data;
            if (enumerable) {
                data = JsonSerializer.deserializeArrayFromInstance(instanceType, data);
            } else {
                data = JsonSerializer.deserializeFromInstance(instanceType, data);
            }

            this.data = data;
        } else {
            this.data = null as any;
        }
    }

    /** @inheritdoc */
    readonly data: TDataType;

    /** @inheritdoc */
    readonly isSuccess: boolean;

    /** @inheritdoc */
    readonly isAuthorized: boolean;

    /** @inheritdoc */
    readonly isValid: boolean;

    /** @inheritdoc */
    readonly hasExceptions: boolean;

    /** @inheritdoc */
    readonly validationResults: ValidationResult[];

    /** @inheritdoc */
    readonly exceptionMessages: string[];

    /** @inheritdoc */
    readonly exceptionStackTrace: string;

    /**
     * Gets whether or not the query has data.
     */
    get hasData(): boolean {
        const data = this.data as any;
        if (data) {
            if (data.constructor && data.constructor === Array) {
                if (data.length || 0 > 0) {
                    return true;
                }
            } else {
                return true;
            }
        }

        return false;
    }
}
