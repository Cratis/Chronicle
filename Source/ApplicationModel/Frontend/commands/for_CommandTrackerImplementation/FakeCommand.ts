// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { CommandResult } from '../CommandResult';
import { ICommand, PropertyChanged } from '../ICommand';

export class FakeCommand implements ICommand {
    route = '';

    constructor(readonly hasChanges: boolean) {
        this.execute = sinon.stub();
        this.setInitialValues = sinon.stub();
        this.setInitialValues = sinon.stub();
        this.propertyChanged = sinon.stub();
        this.onPropertyChanged = sinon.stub();
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
