// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICommand, PropertyChanged } from './ICommand';
import { CommandResult } from "./CommandResult";
import { CommandValidator } from './CommandValidator';


type Callback = {
    callback: WeakRef<PropertyChanged>;
    thisArg: WeakRef<any>;
}

/**
 * Represents an implementation of {@link ICommand} that works with HTTP fetch.
 */
export abstract class Command<TCommandContent = {}> implements ICommand<TCommandContent> {
    abstract readonly route: string;
    abstract readonly routeTemplate: Handlebars.TemplateDelegate;
    abstract readonly validation: CommandValidator;
    abstract get requestArguments(): string[];
    abstract get properties(): string[];

    private _initialValues: any = {};
    private _hasChanges = false;
    private _callbacks: Callback[] = [];

    /** @inheritdoc */
    async execute(): Promise<CommandResult> {
        let actualRoute = this.route;
        const payload = {};

        this.properties.forEach(property => {
            payload[property] = this[property];
        });

        if (this.requestArguments && this.requestArguments.length > 0) {
            actualRoute = this.routeTemplate(payload);
        }

        const response = await fetch(actualRoute, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });
        this.setInitialValuesFromCurrentValues();

        const result = await response.json();

        return new CommandResult(result);
    }

    /** @inheritdoc */
    setInitialValues(values: TCommandContent) {
        this.properties.forEach(property => {
            if ((values as any).hasOwnProperty(property)) {
                this._initialValues[property] = values[property];
                this[property] = values[property];
            }
        });
        this.updateHasChanges();
    }

    /** @inheritdoc */
    setInitialValuesFromCurrentValues() {
        this.properties.forEach(property => {
            if (this[property]) {
                this._initialValues[property] = this[property];
            }
        });
        this.updateHasChanges();
    }

    /** @inheritdoc */
    revertChanges(): void {
        this.properties.forEach(property => {
            this[property] = this._initialValues[property];
        });
    }

    /** @inheritdoc */
    get hasChanges() {
        return this._hasChanges;
    }

    /** @inheritdoc */
    propertyChanged(property: string) {
        this.updateHasChanges();

        this._callbacks.forEach(callbackContainer => {
            const callback = callbackContainer.callback.deref();
            const thisArg = callbackContainer.thisArg.deref();
            if (callback && thisArg) {
                callback.call(thisArg, property);
            } else {
                this._callbacks = this._callbacks.filter(_ => _.callback !== callbackContainer.callback);
            }
        });
    }

    /** @inheritdoc */
    onPropertyChanged(callback: PropertyChanged, thisArg: any) {
        this._callbacks.push({
            callback: new WeakRef(callback),
            thisArg: new WeakRef(thisArg)
        });
    }

    private updateHasChanges() {
        this._hasChanges = this.properties.some(property => this[property] !== this._initialValues[property]);
    }
}
