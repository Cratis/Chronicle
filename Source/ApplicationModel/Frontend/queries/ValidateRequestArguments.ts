// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Validate request arguments.
 * @param {string} requestName Name of request.
 * @param {string[]} expectedRequestArguments Array of all expected arguments.
 * @param {*} actualArguments Object containing the arguments.
 * @returns True if valid, false if not.
 */
export function ValidateRequestArguments(requestName: string, expectedRequestArguments: string[], actualArguments?: any): boolean {
    if (expectedRequestArguments.length > 0) {
        const missing: string[] = [];

        if (!actualArguments) {
            expectedRequestArguments.forEach(_ => missing.push(_));
        } else {
            for (const argument of expectedRequestArguments) {
                if (!actualArguments.hasOwnProperty(argument) || !actualArguments[argument]) {
                    missing.push(argument);
                }
            }
        }

        if (missing.length > 0) {
            const missingArgumentsString = missing.join(', ');
            console.log(`Warning: Missing (${missingArgumentsString}) arguments for request (${requestName}). Will not perform.`);
            return false;
        }
    }

    return true;
}
