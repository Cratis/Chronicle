// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICommand, PropertyChanged } from './ICommand';
import { CommandResult } from "./CommandResult";
import { CommandValidator } from './CommandValidator';

/**
 * Represents an implementation of {@link ICommand} that works with HTTP fetch.
 */
export abstract class Command implements ICommand {
    abstract readonly route: string;
    abstract readonly routeTemplate: Handlebars.TemplateDelegate;
    abstract readonly validation: CommandValidator;
    abstract get requestArguments(): string[];
    abstract get properties(): string[];

    private _initialValues: any = {};
    private _hasChanges = false;
    private _callbacks: WeakRef<PropertyChanged>[] = [];

    /** @inheritdoc */
    async execute(): Promise<CommandResult> {
        let actualRoute = this.route;
        if (this.requestArguments && this.requestArguments.length > 0) {
            actualRoute = this.routeTemplate(this);
        }

        const response = await fetch(actualRoute, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(this)
        });

        this.setInitialValuesFromCurrentValues();

        return new CommandResult(response);
    }

    /** @inheritdoc */
    setInitialValues(values: any) {
        this.properties.forEach(property => {
            if (values.hasOwnProperty(property)) {
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
    get hasChanges() {
        return this._hasChanges;
    }

    /** @inheritdoc */
    propertyChanged(property: string) {
        this.updateHasChanges();

        this._callbacks.forEach(callbackRef => {
            const callback = callbackRef.deref();
            if (callback) {
                callback(property);
            } else {
                this._callbacks = this._callbacks.filter(_ => _ !== callbackRef);
            }
        });
    }

    /** @inheritdoc */
    onPropertyChanged(callback: PropertyChanged) {
        this._callbacks.push(new WeakRef(callback));
    }

    private updateHasChanges() {
        this._hasChanges = this.properties.some(property => this[property] !== this._initialValues[property]);
    }
}
