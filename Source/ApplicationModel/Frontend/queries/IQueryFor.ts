// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from './QueryResult';
import Handlebars from 'handlebars';

/**
 * Defines the base of a query.
 * @template TDataType Type of model the query is for.
 * @template TArguments Optional type of arguments to use for the query.
 */
export interface IQueryFor<TDataType, TArguments = {}> {
    readonly route: string;
    readonly routeTemplate: Handlebars.TemplateDelegate;
    readonly requestArguments: string[];
    readonly defaultValue: TDataType;

    /**
     * Perform the query.
     * @param [args] Optional arguments for the query - depends on whether or not the query needs arguments.
     * @returns {QueryResult} for the model
     */
    perform(args?: TArguments): Promise<QueryResult<TDataType>>;
}
