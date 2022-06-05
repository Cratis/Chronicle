// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { CommandResult } from '../CommandResult';
import { ICommand, PropertyChanged } from '../ICommand';

export class FakeCommand implements ICommand {
    route = '';
    private _hasChanges: boolean;

    constructor(hasChanges: boolean) {
        this._hasChanges = hasChanges;
        this.execute = sinon.fake(() => {
            this._hasChanges = false;
            return new Promise<CommandResult>(resolve => {
                resolve(new CommandResult({
                    isSuccess: true,
                    isAuthorized: true,
                    isValid: true,
                    hasExceptions: false
                } as any));
            });
        });
        this.setInitialValues = sinon.stub();
        this.setInitialValues = sinon.stub();
        this.propertyChanged = sinon.stub();
        this.onPropertyChanged = sinon.stub();
        this.revertChanges = sinon.fake(() => {
            this._hasChanges = false;
        });
    }

    get hasChanges() {
        return this._hasChanges;
    }

    revertChanges(): void {
        throw new Error('Method not implemented.');
    }

    execute(): Promise<CommandResult> {
        throw new Error('Method not implemented.');
    }
    setInitialValues(values: {}): void {
        throw new Error('Method not implemented.');
    }
    setInitialValuesFromCurrentValues(): void {
        throw new Error('Method not implemented.');
    }
    propertyChanged(property: string): void {
        throw new Error('Method not implemented.');
    }
    onPropertyChanged(callback: PropertyChanged, thisArg: any): void {
        throw new Error('Method not implemented.');
    }
}
