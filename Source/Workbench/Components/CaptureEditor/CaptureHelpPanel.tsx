// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import strings from 'Strings';

export const CaptureHelpPanel: React.FC = () => {
    return (
        <div style={{ padding: '20px', height: '100%', overflowY: 'auto', backgroundColor: '#1e1e1e', color: '#d4d4d4' }}>
            <h2 style={{ color: '#ffffff', fontWeight: 600, marginTop: 0, marginBottom: '20px' }}>
                {strings.components.captureEditor.languageReference}
            </h2>

            <div style={{ borderBottom: '1px solid #3e3e42', marginBottom: '20px' }} />

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Basic Structure
                </h3>
                <pre style={{ backgroundColor: '#2d2d30', padding: '12px', borderRadius: '4px', fontSize: '13px', margin: 0 }}>
{`capture CustomerCapture
  source api
    url https://example.com/customers
  key id
  map
    firstName = first_name
  append CustomerChanged
    when firstName
    customerId = $.id`}
                </pre>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Source Types
                </h3>
                <pre style={{ backgroundColor: '#2d2d30', padding: '12px', borderRadius: '4px', fontSize: '13px', margin: 0 }}>
{`source api
  url https://example.com/items
  poll 5m
  auth bearer $env.API_TOKEN

source webhook
  path /captures/items
  auth bearer $env.WEBHOOK_TOKEN

source message
  topic items.updated`}
                </pre>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Keywords
                </h3>
                <div style={{ fontSize: '13px' }}>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>capture</strong> - Start a capture declaration</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>source</strong> - Define the capture source type</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>key</strong> - Identify the item key</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>map</strong> - Normalize incoming values</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>append</strong> - Declare an event to append</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>when</strong> - Define append conditions</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>nested</strong> - Map nested objects</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>children</strong> - Work with child collections</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>identified by</strong> - Define the child identity property</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>api</strong>, <strong style={{ color: '#569cd6' }}>webhook</strong>, <strong style={{ color: '#569cd6' }}>message</strong> - Supported source kinds</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>url</strong>, <strong style={{ color: '#569cd6' }}>poll</strong>, <strong style={{ color: '#569cd6' }}>auth</strong>, <strong style={{ color: '#569cd6' }}>path</strong>, <strong style={{ color: '#569cd6' }}>topic</strong> - Source configuration keywords</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>from</strong>, <strong style={{ color: '#569cd6' }}>to</strong>, <strong style={{ color: '#569cd6' }}>or</strong>, <strong style={{ color: '#569cd6' }}>and</strong>, <strong style={{ color: '#569cd6' }}>added</strong>, <strong style={{ color: '#569cd6' }}>removed</strong> - When clause keywords</div>
                    <div style={{ marginBottom: '5px' }}><strong style={{ color: '#569cd6' }}>translate</strong>, <strong style={{ color: '#569cd6' }}>split</strong>, <strong style={{ color: '#569cd6' }}>bearer</strong> - Mapping and auth helpers</div>
                </div>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Map Block
                </h3>
                <pre style={{ backgroundColor: '#2d2d30', padding: '12px', borderRadius: '4px', fontSize: '13px', margin: 0 }}>
{`map
  firstName = fornavn
  fullName = "${'${$.firstName} ${$.lastName}'}"
  status = status translate
    "aktiv" => active
    "inaktiv" => inactive
  split fullName by " "
    firstName
    lastName`}
                </pre>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Append Block
                </h3>
                <pre style={{ backgroundColor: '#2d2d30', padding: '12px', borderRadius: '4px', fontSize: '13px', margin: 0 }}>
{`append ItemStatusChanged
  when status
  itemId = $.id
  status = $.status
  changedAt = $context.occurred`}
                </pre>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    When Clause Variants
                </h3>
                <pre style={{ backgroundColor: '#2d2d30', padding: '12px', borderRadius: '4px', fontSize: '13px', margin: 0 }}>
{`when status
when status from inactive to active
when status from * to inactive
when added
when removed
when status or quantity
when status and lineNumber`}
                </pre>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Expressions
                </h3>
                <div style={{ fontSize: '13px' }}>
                    <div style={{ marginBottom: '5px' }}><strong>$.</strong> - Access the current captured value with dot notation</div>
                    <div style={{ marginBottom: '5px' }}><strong>$previous.</strong> - Access the previous captured value</div>
                    <div style={{ marginBottom: '5px' }}><strong>$context.</strong> - Access context metadata like occurred and eventSourceId</div>
                    <div style={{ marginBottom: '5px' }}><strong>$env.</strong> - Read environment variables such as $env.API_TOKEN</div>
                </div>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <h3 style={{ color: '#4ec9b0', fontWeight: 600, marginTop: 0, marginBottom: '10px' }}>
                    Tips
                </h3>
                <div style={{ fontSize: '13px' }}>
                    <div style={{ marginBottom: '5px' }}>• Use <strong>Tab</strong> for indentation (2 spaces)</div>
                    <div style={{ marginBottom: '5px' }}>• Press <strong>Ctrl+Space</strong> for auto-completion</div>
                    <div style={{ marginBottom: '5px' }}>• Hover over keywords and built-ins for documentation</div>
                    <div style={{ marginBottom: '5px' }}>• Completions are context-sensitive inside source, map, and append blocks</div>
                </div>
            </div>
        </div>
    );
};
