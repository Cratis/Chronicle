// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import * as faIcons from 'react-icons/fa6';
import type { Json } from 'Features';

interface InstanceCellProps {
    value: Json | undefined;
    onNavigate: () => void;
}

const NavigationLink = ({ label, onNavigate }: { label: string; onNavigate: () => void }) => (
    <div
        className="flex align-items-center gap-2 cursor-pointer"
        onClick={(e) => { e.stopPropagation(); onNavigate(); }}
        style={{ color: 'var(--primary-color)', display: 'flex', alignItems: 'center' }}
    >
        <span>{label}</span>
        <faIcons.FaArrowRight style={{ fontSize: '0.875rem', display: 'inline-flex' }} />
    </div>
);

/** Shows a value of a row: arrays and objects as a link to browse into, anything else as text. */
export const InstanceCell = ({ value, onNavigate }: InstanceCellProps) => {
    if (value === null || value === undefined) return '';
    if (Array.isArray(value)) {
        return <NavigationLink label={`${strings.eventStore.namespaces.readModels.labels.array}[${value.length}]`} onNavigate={onNavigate} />;
    }
    if (typeof value === 'object') {
        return <NavigationLink label={strings.eventStore.namespaces.readModels.labels.object} onNavigate={onNavigate} />;
    }
    return String(value);
};
