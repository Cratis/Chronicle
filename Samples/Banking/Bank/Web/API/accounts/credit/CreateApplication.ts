/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/credit');

export interface ICreateApplication {
}

export class CreateApplicationValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
    };
}

export class CreateApplication extends Command<ICreateApplication> implements ICreateApplication {
    readonly route: string = '/api/accounts/credit';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new CreateApplicationValidator();


    get requestArguments(): string[] {
        return [
        ];
    }

    get properties(): string[] {
        return [
        ];
    }


    static use(initialValues?: ICreateApplication): [CreateApplication, SetCommandValues<ICreateApplication>] {
        return useCommand<CreateApplication, ICreateApplication>(CreateApplication, initialValues);
    }
}
