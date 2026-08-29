// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Dropdown } from '@cratis/components/Dropdown';
import strings from 'Strings';

export interface PatternScopeSelectorProps {
    scopes: string[];
    selected?: string;
    onChange: (scope: string) => void;
}

/**
 * Selects which scope's behavior is being looked at.
 *
 * Patterns are per scope, so nothing can be shown until one is chosen. A view that silently picked the first
 * scope would read as "this is the store's behavior" when it is one person's.
 */
export const PatternScopeSelector = ({ scopes, selected, onChange }: PatternScopeSelectorProps) => (
    <div className="flex items-center gap-2">
        <label htmlFor="pattern-scope">{strings.patterns.scope}</label>
        {selected
            // Mounted only once a scope is settled on. The underlying select takes its value as uncontrolled when
            // it first renders without one, and then ignores the value arriving a moment later - so it would sit
            // there showing the placeholder while everything below it displayed that scope's patterns.
            ? (
                <Dropdown<string>
                    id="pattern-scope"
                    value={selected}
                    options={scopes}
                    filter={scopes.length > 10}
                    placeholder={strings.patterns.scope}
                    className="min-w-[20rem]"
                    onChange={(event) => onChange(event.value)}
                />
            )
            : <span className="opacity-70">{strings.patterns.noScopes}</span>}
    </div>
);
