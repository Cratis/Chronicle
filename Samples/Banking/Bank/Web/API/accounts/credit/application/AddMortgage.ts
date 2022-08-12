/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/credit/application/{{applicationId}}/mortgage/{{remaining}}');

export interface IAddMortgage {
    applicationId?: string;
    remaining?: number;
}

export class AddMortgageValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        applicationId: new Validator(),
        remaining: new Validator(),
    };
}

export class AddMortgage extends Command<IAddMortgage> implements IAddMortgage {
    readonly route: string = '/api/accounts/credit/application/{{applicationId}}/mortgage/{{remaining}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new AddMortgageValidator();

    private _applicationId!: string;
    private _remaining!: number;

    get requestArguments(): string[] {
        return [
            'applicationId',
            'remaining',
        ];
    }

    get properties(): string[] {
        return [
            'applicationId',
            'remaining',
        ];
    }

    get applicationId(): string {
        return this._applicationId;
    }

    set applicationId(value: string) {
        this._applicationId = value;
        this.propertyChanged('applicationId');
    }
    get remaining(): number {
        return this._remaining;
    }

    set remaining(value: number) {
        this._remaining = value;
        this.propertyChanged('remaining');
    }

    static use(initialValues?: IAddMortgage): [AddMortgage, SetCommandValues<IAddMortgage>] {
        return useCommand<AddMortgage, IAddMortgage>(AddMortgage, initialValues);
    }
}
