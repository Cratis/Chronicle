/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/credit/application/{{applicationId}}/income/{{income}}');

export interface IRegisterYearlyIncome {
    applicationId?: string;
    income?: number;
}

export class RegisterYearlyIncomeValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        applicationId: new Validator(),
        income: new Validator(),
    };
}

export class RegisterYearlyIncome extends Command<IRegisterYearlyIncome> implements IRegisterYearlyIncome {
    readonly route: string = '/api/accounts/credit/application/{{applicationId}}/income/{{income}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new RegisterYearlyIncomeValidator();

    private _applicationId!: string;
    private _income!: number;

    get requestArguments(): string[] {
        return [
            'applicationId',
            'income',
        ];
    }

    get properties(): string[] {
        return [
            'applicationId',
            'income',
        ];
    }

    get applicationId(): string {
        return this._applicationId;
    }

    set applicationId(value: string) {
        this._applicationId = value;
        this.propertyChanged('applicationId');
    }
    get income(): number {
        return this._income;
    }

    set income(value: number) {
        this._income = value;
        this.propertyChanged('income');
    }

    static use(initialValues?: IRegisterYearlyIncome): [RegisterYearlyIncome, SetCommandValues<IRegisterYearlyIncome>] {
        return useCommand<RegisterYearlyIncome, IRegisterYearlyIncome>(RegisterYearlyIncome, initialValues);
    }
}
