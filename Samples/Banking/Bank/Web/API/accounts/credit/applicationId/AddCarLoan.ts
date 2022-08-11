/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/credit/applicationId/carloan/{{remaining}}');

export interface IAddCarLoan {
    applicationId?: string;
    remaining?: number;
}

export class AddCarLoanValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        applicationId: new Validator(),
        remaining: new Validator(),
    };
}

export class AddCarLoan extends Command<IAddCarLoan> implements IAddCarLoan {
    readonly route: string = '/api/accounts/credit/applicationId/carloan/{{remaining}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new AddCarLoanValidator();

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

    static use(initialValues?: IAddCarLoan): [AddCarLoan, SetCommandValues<IAddCarLoan>] {
        return useCommand<AddCarLoan, IAddCarLoan>(AddCarLoan, initialValues);
    }
}
