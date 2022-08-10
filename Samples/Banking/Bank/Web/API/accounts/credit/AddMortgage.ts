/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/credit/{{remaining}}');

export interface IAddMortgage {
    remaining?: number;
}

export class AddMortgageValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        remaining: new Validator(),
    };
}

export class AddMortgage extends Command<IAddMortgage> implements IAddMortgage {
    readonly route: string = '/api/accounts/credit/{{remaining}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new AddMortgageValidator();

    private _remaining!: number;

    get requestArguments(): string[] {
        return [
            'remaining',
        ];
    }

    get properties(): string[] {
        return [
            'remaining',
        ];
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
