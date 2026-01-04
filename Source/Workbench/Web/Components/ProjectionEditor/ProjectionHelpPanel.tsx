// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';

export const ProjectionHelpPanel: React.FC = () => {
    return (
        <div style={{ padding: '20px', height: '100%', overflowY: 'auto', backgroundColor: '#1e1e1e', color: '#d4d4d4' }}>
            <h2 style={{ color: '#ffffff', fontWeight: 600, marginTop: 0, marginBottom: '20px' }}>
                Projection DSL Quick Reference
            </h2>

            <div style={{ borderBottom: '1px solid #3e3e42', marginBottom: '20px' }} />

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Basic Structure
                </h3>
                <pre style={{ backgroundColor: '#2d2d30', padding: '12px', borderRadius: '4px', fontSize: '13px', margin: 0 }}>
{`projection MyReadModel
    from MyEvent
        set property = eventProperty`}
                </pre>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Keywords
                </h3>
                <div style={{ fontSize: '13px' }}>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>projection</strong> - Define a projection and its target read model</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>from</strong> - Specify event type to project from</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>every</strong> - Project from a sequence of events</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>set</strong> - Set a property value</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>key</strong> - Define the read model key</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>join</strong> - Join with another read model</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>children</strong> - Define child collection</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>increment</strong> - Increase a numeric value</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>decrement</strong> - Decrease a numeric value</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>count</strong> - Count occurrences</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>add</strong> - Add to a collection</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>subtract</strong> - Remove from a collection</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>remove</strong> - Remove the read model instance</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>automap</strong> - Automatically map matching properties</div>
                </div>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Expressions
                </h3>
                <div style={{ fontSize: '13px' }}>
                    <div style={{ marginBottom: '5px' }}><strong>$parent.</strong> - Access parent read model properties</div>
                    <div style={{ marginBottom: '5px' }}><strong>$child.</strong> - Access child properties</div>
                    <div style={{ marginBottom: '5px' }}><strong>$eventContext.</strong> - Access event metadata (correlationId, causationId, occurred)</div>
                    <div style={{ marginBottom: '5px' }}><strong>$causedBy.</strong> - Access identity info (subject, name, userName)</div>
                    <div style={{ marginBottom: '5px' }}><strong>$count</strong> - Current count value</div>
                </div>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Key Patterns
                </h3>
                <pre style={{ backgroundColor: '#2d2d30', padding: '12px', borderRadius: '4px', fontSize: '13px', margin: 0 }}>
{`// Simple key
key userId

// Composite key
key
    userId = userId
    tenantId = tenantId

// Parent key
parent key parentId`}
                </pre>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Common Patterns
                </h3>
                <pre style={{ backgroundColor: '#2d2d30', padding: '12px', borderRadius: '4px', fontSize: '13px', margin: 0 }}>
{`// Auto-map properties
from MyEvent
    automap

// Count events
from MyEvent
    count eventCount

// Join to another model
join OtherModel with otherId
    set otherName = $child.name

// Child collection
children Items
    from ItemAdded
        add item
            id = itemId
            name = itemName`}
                </pre>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Tips
                </h3>
                <div style={{ fontSize: '13px' }}>
                    <div style={{ marginBottom: '5px' }}>• Use <strong>Tab</strong> for indentation (2 spaces)</div>
                    <div style={{ marginBottom: '5px' }}>• Press <strong>Ctrl+Space</strong> for auto-completion</div>
                    <div style={{ marginBottom: '5px' }}>• Hover over keywords for documentation</div>
                    <div style={{ marginBottom: '5px' }}>• Auto-completion is context-sensitive</div>
                    <div style={{ marginBottom: '5px' }}>• Errors appear inline with red markers</div>
                    <div style={{ marginBottom: '5px' }}>• Use <strong>causedBy</strong> for identity tracking</div>
                    <div style={{ marginBottom: '5px' }}>• Use <strong>eventContext</strong> for metadata</div>
                </div>
            </div>

            <div style={{ borderBottom: '1px solid #3e3e42', marginBottom: '20px' }} />

            <div>
                <a href="https://github.com/Cratis/Chronicle" target="_blank" rel="noopener noreferrer" style={{ fontSize: '13px', color: '#4ec9b0', display: 'block', marginBottom: '5px' }}>
                    Documentation
                </a>
                <a href="https://github.com/Cratis/Chronicle/tree/main/Documentation/projections/projection-definition-language" target="_blank" rel="noopener noreferrer" style={{ fontSize: '13px', color: '#4ec9b0', display: 'block' }}>
                    DSL Reference
                </a>
            </div>
        </div>
    );
};
