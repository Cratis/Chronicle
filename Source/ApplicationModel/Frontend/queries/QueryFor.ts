// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IQueryFor } from './IQueryFor';
import { QueryResult } from "./QueryResult";
import Handlebars from 'handlebars';

/**
 * Represents an implementation of {@link IQueryFor}.
 * @template TDataType Type of data returned by the query.
 */
export abstract class QueryFor<TDataType, TArguments = {}> implements IQueryFor<TDataType, TArguments> {
    abstract readonly route: string;
    abstract readonly routeTemplate: Handlebars.TemplateDelegate;

    abstract defaultValue: TDataType;
    abstract requiresArguments: boolean;

    /** @inheritdoc */
    async perform(args?: TArguments): Promise<QueryResult<TDataType>> {
        let actualRoute = this.route;
        if (args && Object.keys(args).length > 0) {
            actualRoute = this.routeTemplate(args);
        }

        const response = await fetch(actualRoute, {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });

        return await QueryResult.fromResponse<TDataType>(response);
    }
}
