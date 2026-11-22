// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';

export interface HeaderSummaryProps {
    headers?: Readonly<Record<string, string>>;
    headerValuesSet?: Readonly<Record<string, boolean>>;
}

const valueStatus = (isSet: boolean | undefined) => {
    switch (isSet) {
        case true: return strings.general.headers.valueSet;
        case false: return strings.general.headers.notSet;
        default: return strings.general.headers.unavailable;
    }
};

export const HeaderSummary = ({ headers, headerValuesSet }: HeaderSummaryProps) => (
    <ul>
        {Object.keys(headers ?? {}).map((name) => (
            <li key={name}><strong>{name}:</strong> {valueStatus(headerValuesSet?.[name])}</li>
        ))}
    </ul>
);
