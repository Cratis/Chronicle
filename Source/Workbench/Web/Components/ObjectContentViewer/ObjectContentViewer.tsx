// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Json } from 'Features/index';
import { Tooltip } from 'primereact/tooltip';
import React, { useState, useCallback, useMemo } from 'react';
import * as faIcons from 'react-icons/fa6';
import strings from 'Strings';
import { ObjectNavigationalBar } from 'Components';
import { JsonSchema, JsonSchemaProperty } from 'Components/JsonSchema';


export interface ObjectContentViewerProps {
    object: Json;
    timestamp?: Date;
    schema: JsonSchema;
}

export const ObjectContentViewer = ({ object, timestamp, schema }: ObjectContentViewerProps) => {
    const [navigationPath, setNavigationPath] = useState<string[]>([]);

    const getValueAtPath = useCallback((data: Json, path: string[]): Json | null => {
        let current: Json = data;
        for (const segment of path) {
            if (current === null || current === undefined) return null;
            if (typeof current === 'object' && !Array.isArray(current) && current !== null) {
                current = (current as { [key: string]: Json })[segment];
            } else {
                return null;
            }
        }
        return current;
    }, []);

    const navigateToProperty = useCallback((key: string) => {
        setNavigationPath([...navigationPath, key]);
    }, [navigationPath]);

    const navigateToBreadcrumb = useCallback((index: number) => {
        if (index === 0) {
            setNavigationPath([]);
        } else {
            setNavigationPath(navigationPath.slice(0, index));
        }
    }, [navigationPath]);

    const currentData = useMemo(() => {
        if (navigationPath.length === 0) {
            return object;
        }

        const lastKey = navigationPath[navigationPath.length - 1];
        const pathToParent = navigationPath.slice(0, -1);

        const parentValue = pathToParent.length > 0
            ? getValueAtPath(object, pathToParent)
            : object;

        if (parentValue && typeof parentValue === 'object' && !Array.isArray(parentValue)) {
            const value = (parentValue as { [k: string]: Json })[lastKey];

            if (Array.isArray(value)) {
                return value;
            } else if (value && typeof value === 'object') {
                return value;
            }
        }

        return object;
    }, [object, navigationPath, getValueAtPath]);

    const currentProperties = useMemo(() => {
        const properties = schema.properties || {};

        if (navigationPath.length === 0) {
            return properties;
        }

        return {};
    }, [schema, navigationPath]);

    const tableStyle: React.CSSProperties = {
        width: '100%',
        borderCollapse: 'collapse',
        fontFamily: '-apple-system, BlinkMacSystemFont, "SF Mono", monospace',
        fontSize: '13px',
    };

    const rowStyle: React.CSSProperties = {
        borderBottom: '1px solid rgba(255,255,255,0.1)',
    };

    const labelStyle: React.CSSProperties = {
        padding: '8px 12px',
        color: 'rgba(255,255,255,0.6)',
        textAlign: 'left',
        fontWeight: 500,
        width: '140px',
    };

    const valueStyle: React.CSSProperties = {
        padding: '8px 12px',
        color: '#fff',
        textAlign: 'left',
    };

    const infoIconStyle: React.CSSProperties = {
        marginLeft: '6px',
        fontSize: '12px',
        color: 'rgba(100, 150, 255, 0.6)',
        cursor: 'help',
    };

    const renderValue = (value: Json, propertyName: string) => {
        if (value === null || value === undefined) return '';

        if (Array.isArray(value)) {
            return (
                <div
                    className="flex align-items-center gap-2 cursor-pointer"
                    onClick={() => navigateToProperty(propertyName)}
                    style={{ color: 'var(--primary-color)', display: 'flex', alignItems: 'center' }}
                >
                    <span>{strings.eventStore.namespaces.readModels.labels.array}[{value.length}]</span>
                    <faIcons.FaArrowRight style={{ fontSize: '0.875rem', display: 'inline-flex' }} />
                </div>
            );
        }

        if (typeof value === 'object') {
            return (
                <div
                    className="flex align-items-center gap-2 cursor-pointer"
                    onClick={() => navigateToProperty(propertyName)}
                    style={{ color: 'var(--primary-color)', display: 'flex', alignItems: 'center' }}
                >
                    <span>{strings.eventStore.namespaces.readModels.labels.object}</span>
                    <faIcons.FaArrowRight style={{ fontSize: '0.875rem', display: 'inline-flex' }} />
                </div>
            );
        }

        return String(value);
    };

    const renderTable = () => {
        if (Array.isArray(currentData)) {
            if (currentData.length === 0) return <div style={{ padding: '12px', color: 'rgba(255,255,255,0.6)' }}>Empty array</div>;

            const firstItem = currentData[0];
            if (typeof firstItem === 'object' && firstItem !== null && !Array.isArray(firstItem)) {
                const keys = Object.keys(firstItem);

                return (
                    <table style={tableStyle}>
                        <tbody>
                            {currentData.map((item, index) => (
                                <React.Fragment key={index}>
                                    {index > 0 && (
                                        <tr style={{ height: '8px', background: 'rgba(255,255,255,0.05)' }}>
                                            <td colSpan={2}></td>
                                        </tr>
                                    )}
                                    {keys.map((key) => (
                                        <tr key={`${index}-${key}`} style={rowStyle}>
                                            <td style={labelStyle}>{key}</td>
                                            <td style={valueStyle}>{renderValue((item as Record<string, Json>)[key], key)}</td>
                                        </tr>
                                    ))}
                                </React.Fragment>
                            ))}
                        </tbody>
                    </table>
                );
            } else {
                return (
                    <table style={tableStyle}>
                        <tbody>
                            {currentData.map((item, index) => (
                                <tr key={index} style={rowStyle}>
                                    <td style={labelStyle}>[{index}]</td>
                                    <td style={valueStyle}>{renderValue(item, `[${index}]`)}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                );
            }
        }

        const entries = navigationPath.length === 0
            ? Object.entries(currentProperties)
            : Object.entries(currentData as { [key: string]: Json });

        return (
            <table style={tableStyle}>
                <tbody>
                    {entries.map(([propertyName, propertyDef]: [string, JsonSchemaProperty | Json]) => {
                        const value = (currentData as Record<string, Json>)[propertyName];

                        const isSchemaProperty = navigationPath.length === 0;
                        const description = isSchemaProperty && typeof propertyDef === 'object' && propertyDef !== null && 'description' in propertyDef
                            ? (propertyDef as JsonSchemaProperty).description
                            : undefined;

                        return (
                            <tr key={propertyName} style={rowStyle}>
                                <td style={labelStyle}>
                                    {propertyName}
                                    {description && (
                                        <i
                                            className="pi pi-info-circle property-info-icon"
                                            style={infoIconStyle}
                                            data-pr-tooltip={description} />
                                    )}
                                </td>
                                <td style={valueStyle}>{renderValue(value as Json, propertyName)}</td>
                            </tr>
                        );
                    })}
                </tbody>
            </table>
        );
    };

    return (
        <div className="order-content" style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            <Tooltip target=".property-info-icon" />
            <ObjectNavigationalBar
                navigationPath={navigationPath}
                onNavigate={navigateToBreadcrumb}
            />
            {renderTable()}
            {timestamp && (
                <div style={{
                    marginTop: '20px',
                    padding: '12px',
                    background: 'rgba(100, 150, 255, 0.1)',
                    borderRadius: '8px',
                    fontSize: '12px',
                    color: 'rgba(255,255,255,0.6)'
                }}>
                    Snapshot captured: {timestamp.toLocaleString()}
                </div>
            )}
        </div>
    );
};
