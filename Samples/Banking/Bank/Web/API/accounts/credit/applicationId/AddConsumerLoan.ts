/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/credit/applicationId/consumerloan/{{remaining}}');

export interface IAddConsumerLoan {
    applicationId?: string;
    remaining?: number;
}

export class AddConsumerLoanValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        applicationId: new Validator(),
        remaining: new Validator(),
    };
}

export class AddConsumerLoan extends Command<IAddConsumerLoan> implements IAddConsumerLoan {
    readonly route: string = '/api/accounts/credit/applicationId/consumerloan/{{remaining}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new AddConsumerLoanValidator();

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

    static use(initialValues?: IAddConsumerLoan): [AddConsumerLoan, SetCommandValues<IAddConsumerLoan>] {
        return useCommand<AddConsumerLoan, IAddConsumerLoan>(AddConsumerLoan, initialValues);
    }
}
