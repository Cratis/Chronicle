/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/credit/{{remaining}}');

export interface IAddPersonalLoan {
    remaining?: number;
}

export class AddPersonalLoanValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        remaining: new Validator(),
    };
}

export class AddPersonalLoan extends Command<IAddPersonalLoan> implements IAddPersonalLoan {
    readonly route: string = '/api/accounts/credit/{{remaining}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new AddPersonalLoanValidator();

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

    static use(initialValues?: IAddPersonalLoan): [AddPersonalLoan, SetCommandValues<IAddPersonalLoan>] {
        return useCommand<AddPersonalLoan, IAddPersonalLoan>(AddPersonalLoan, initialValues);
    }
}
