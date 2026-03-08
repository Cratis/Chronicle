import{j as e}from"./Dialog-DzzX7THe.js";import{D as n}from"./DialogComponents-D1jl3m-J.js";import"./iframe-CvpRhCjh.js";import{D as a,B as i,C as l}from"./Dialog-B2gb5Jg5.js";import"./index-4rm-Zybi.js";import"./preload-helper-PPVm8Dsz.js";const D={title:"Components/Dialog",component:a,decorators:[o=>e.jsx(n,{confirmation:l,busyIndicator:i,children:e.jsx(o,{})})],tags:["autodocs"]},t={args:{title:"Example Dialog",visible:!0,onClose:o=>{},children:e.jsx("div",{className:"p-4",children:"This is dialog content"})}},s={args:{title:"Confirmation",visible:!0,onClose:o=>{},children:e.jsx("p",{children:"Are you sure you want to proceed?"}),buttons:1}},r={args:{title:"Delete Item",visible:!0,onClose:o=>{},children:e.jsx("p",{children:"Do you want to delete this item? This action cannot be undone."}),buttons:3}};t.parameters={...t.parameters,docs:{...t.parameters?.docs,source:{originalSource:`{
  args: {
    title: 'Example Dialog',
    visible: true,
    onClose: (_result: DialogResult) => {},
    children: <div className="p-4">This is dialog content</div>
  }
}`,...t.parameters?.docs?.source}}};s.parameters={...s.parameters,docs:{...s.parameters?.docs,source:{originalSource:`{
  args: {
    title: 'Confirmation',
    visible: true,
    onClose: (_result: DialogResult) => {},
    children: <p>Are you sure you want to proceed?</p>,
    buttons: 1 // DialogButtons.Ok
  }
}`,...s.parameters?.docs?.source}}};r.parameters={...r.parameters,docs:{...r.parameters?.docs,source:{originalSource:`{
  args: {
    title: 'Delete Item',
    visible: true,
    onClose: (_result: DialogResult) => {},
    children: <p>Do you want to delete this item? This action cannot be undone.</p>,
    buttons: 3 // DialogButtons.YesNo
  }
}`,...r.parameters?.docs?.source}}};const h=["OkCancel","OkOnly","YesNo"];export{t as OkCancel,s as OkOnly,r as YesNo,h as __namedExportsOrder,D as default};
