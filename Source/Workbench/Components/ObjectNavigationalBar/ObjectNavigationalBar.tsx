// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { Button } from 'primereact/button';
import * as faIcons from 'react-icons/fa6';
import strings from 'Strings';

export interface ObjectNavigationalBarProps {
    navigationPath: string[];
    onNavigate: (index: number) => void;
}

export function ObjectNavigationalBar({ navigationPath, onNavigate }: ObjectNavigationalBarProps) {
    const breadcrumbItems = useMemo(() => {
        const items: { name: string; index: number }[] = [{ name: strings.eventStore.namespaces.readModels.labels.root, index: 0 }];
        for (let i = 0; i < navigationPath.length; i++) {
            items.push({
                name: navigationPath[i],
                index: i + 1
            });
        }
        return items;
    }, [navigationPath]);

    return (
        <div className="px-4 py-2 mb-2 border-bottom-1 surface-border">
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <Button
                    icon={<faIcons.FaArrowLeft />}
                    className="p-button-text p-button-sm"
                    onClick={() => onNavigate(navigationPath.length - 1)}
                    tooltip={strings.eventStore.namespaces.readModels.actions.navigateBack}
                    tooltipOptions={{ position: 'top' }}
                    disabled={navigationPath.length === 0}
                />
                <div style={{ fontSize: '0.9rem', color: 'var(--text-color-secondary)' }}>
                    {breadcrumbItems.map((item, index) => (
                        <span key={index}>
                            {index > 0 && <span className="mx-2">&gt;</span>}
                            <span
                                onClick={() => onNavigate(item.index)}
                                style={{
                                    cursor: 'pointer',
                                    textDecoration: index < breadcrumbItems.length - 1 ? 'underline' : 'none'
                                }}
                            >
                                {item.name}
                            </span>
                        </span>
                    ))}
                </div>
            </div>
        </div>
    );
}
