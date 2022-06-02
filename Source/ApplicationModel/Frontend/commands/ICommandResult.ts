// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@aksio/cratis-fundamentals';
import { ValidationError } from '../validation/ValidationError';

/**
 * Defines the result from executing commands.
 */
 export interface ICommandResult {
    /**
     * Gets the correlation identifier associated with the executed command.
     */
     readonly correlationId: Guid;

     /**
      * Gets whether or not the command executed successfully.
      */
     readonly isSuccess: boolean;

     /**
      * Gets whether or not the command was authorized to execute.
      */
     readonly isAuthorized: boolean;

     /**
      * Gets whether or not the command is valid.
      */
     readonly isValid: boolean;

     /**
      * Gets whether or not there are any exceptions that occurred.
      */
     readonly hasExceptions: boolean;

     /**
      * Gets any validation errors. If this collection is empty, there are errors.
      */
     readonly validationErrors: ValidationError[];

     /**
      * Gets any exception messages that might have occurred.
      */
     readonly exceptionMessages: string[];

     /**
      * Gets the stack trace if there was an exception.
      */
     readonly exceptionStackTrace: string;

}
