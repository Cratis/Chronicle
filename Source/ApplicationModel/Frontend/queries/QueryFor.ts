// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IQueryFor } from './IQueryFor';
import { QueryResult } from "./QueryResult";
import Handlebars from 'handlebars';
import { ValidateRequestArguments } from './ValidateRequestArguments';
import { Constructor } from '@aksio/cratis-fundamentals';

/**
 * Represents an implementation of {@link IQueryFor}.
 * @template TDataType Type of data returned by the query.
 */
export abstract class QueryFor<TDataType, TArguments = {}> implements IQueryFor<TDataType, TArguments> {
    abstract readonly route: string;
    abstract readonly routeTemplate: Handlebars.TemplateDelegate;
    abstract get requestArguments(): string[];
    abstract defaultValue: TDataType;

    /**
     * Initializes a new instance of the {@link ObservableQueryFor<,>}} class.
     * @param modelType Type of model, if an enumerable, this is the instance type.
     * @param enumerable Whether or not it is an enumerable.
     */
     constructor(readonly modelType: Constructor, readonly enumerable: boolean) {
    }

    /** @inheritdoc */
    async perform(args?: TArguments): Promise<QueryResult<TDataType>> {
        let actualRoute = this.route;

        if (!ValidateRequestArguments(this.constructor.name, this.requestArguments, args)) {
            return new Promise<QueryResult<TDataType>>((resolve) => {
                resolve(new QueryResult(this.defaultValue, true));
            });
        }

        actualRoute = this.routeTemplate(args);
        const response = await fetch(actualRoute, {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });

        return await QueryResult.fromResponse<TDataType>(response,this.modelType, this.enumerable);
    }
}
