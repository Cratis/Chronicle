// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ReadModelDefinition } from 'Api/ReadModelTypes/ReadModelDefinition';
import { Json } from 'Features/index';
import { Tooltip } from 'primereact/tooltip';
import React from 'react';


export interface ReadModelContentProps {
    readModel: Json;
    timestamp: Date;
    readModelDefinition: ReadModelDefinition;
}

// Component to render the SimulatedOrder as a table
export const ReadModelContent = ({ readModel, timestamp, readModelDefinition }: ReadModelContentProps) => {
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

    // Parse the schema to get property definitions
    const schema = JSON.parse(readModelDefinition.schema);
    const properties = schema.properties || {};

    return (
        <div className="order-content">
            <Tooltip target=".property-info-icon" />
            <table style={tableStyle}>
                <tbody>
                    {Object.entries(properties).map(([propertyName, propertyDef]: [string, any]) => {
                        const value = (readModel as any)[propertyName];
                        const formattedValue = value !== undefined && value !== null
                            ? (typeof value === 'object' ? JSON.stringify(value) : String(value))
                            : '';

                        return (
                            <tr key={propertyName} style={rowStyle}>
                                <td style={labelStyle}>
                                    {propertyName}
                                    {propertyDef.description && (
                                        <i
                                            className="pi pi-info-circle property-info-icon"
                                            style={infoIconStyle}
                                            data-pr-tooltip={propertyDef.description} />
                                    )}
                                </td>
                                <td style={valueStyle}>{formattedValue}</td>
                            </tr>
                        );
                    })}
                </tbody>
            </table>
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
        </div>
    );
};
