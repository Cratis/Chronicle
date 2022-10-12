// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryFor, OnNextResult } from './IObservableQueryFor';
import Handlebars from 'handlebars';
import { ObservableQueryConnection } from './ObservableQueryConnection';
import { ObservableQuerySubscription } from './ObservableQuerySubscription';
import { ValidateRequestArguments } from './ValidateRequestArguments';
import { IObservableQueryConnection } from './IObservableQueryConnection';
import { NullObservableQueryConnection } from './NullObservableQueryConnection';
import { Constructor } from '@aksio/cratis-fundamentals';
import { JsonSerializer } from '@aksio/cratis-fundamentals';

/**
 * Represents an implementation of {@link IQueryFor}.
 * @template TDataType Type of data returned by the query.
 */
export abstract class ObservableQueryFor<TDataType, TArguments = {}> implements IObservableQueryFor<TDataType, TArguments> {
    abstract readonly route: string;
    abstract readonly routeTemplate: Handlebars.TemplateDelegate<any>;
    abstract readonly defaultValue: TDataType;
    abstract get requestArguments(): string[];

    /**
     * Initializes a new instance of the {@link ObservableQueryFor<,>}} class.
     * @param modelType Type of model, if an enumerable, this is the instance type.
     * @param enumerable Whether or not it is an enumerable.
     */
    constructor(readonly modelType: Constructor, readonly enumerable: boolean) {
    }

    /** @inheritdoc */
    subscribe(callback: OnNextResult, args?: TArguments): ObservableQuerySubscription<TDataType> {
        let actualRoute = this.route;
        let connection: IObservableQueryConnection<TDataType>;

        if (!ValidateRequestArguments(this.constructor.name, this.requestArguments, args)) {
            connection = new NullObservableQueryConnection(this.defaultValue);
        } else {
            actualRoute = this.routeTemplate(args);
            connection = new ObservableQueryConnection<TDataType>(actualRoute);
        }

        const subscriber = new ObservableQuerySubscription(connection);
        connection.connect(data => {
            const result: any = data;
            if( this.enumerable) {
                result.data = JsonSerializer.deserializeArrayFromInstance(this.modelType, result.data);
            } else {
                result.data = JsonSerializer.deserializeFromInstance(this.modelType, result.data);
            }
            callback(result);
        });
        return subscriber;
    }
}
