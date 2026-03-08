import{u as R,P as B,a as q,c as T,C as E,j as e,b as C,d as O,D as S,B as i}from"./Dialog-DzzX7THe.js";import{r as l}from"./iframe-CvpRhCjh.js";import{u as V,b as f,a}from"./DialogComponents-D1jl3m-J.js";var Y={root:"p-progress-spinner",spinner:"p-progress-spinner-svg",circle:"p-progress-spinner-circle"},_=`
@layer primereact {
    .p-progress-spinner {
        position: relative;
        margin: 0 auto;
        width: 100px;
        height: 100px;
        display: inline-block;
    }
    
    .p-progress-spinner::before {
        content: '';
        display: block;
        padding-top: 100%;
    }
    
    .p-progress-spinner-svg {
        animation: p-progress-spinner-rotate 2s linear infinite;
        height: 100%;
        transform-origin: center center;
        width: 100%;
        position: absolute;
        top: 0;
        bottom: 0;
        left: 0;
        right: 0;
        margin: auto;
    }
    
    .p-progress-spinner-circle {
        stroke-dasharray: 89, 200;
        stroke-dashoffset: 0;
        stroke: #d62d20;
        animation: p-progress-spinner-dash 1.5s ease-in-out infinite, p-progress-spinner-color 6s ease-in-out infinite;
        stroke-linecap: round;
    }
}

@keyframes p-progress-spinner-rotate {
    100% {
        transform: rotate(360deg);
    }
}

@keyframes p-progress-spinner-dash {
    0% {
        stroke-dasharray: 1, 200;
        stroke-dashoffset: 0;
    }
    50% {
        stroke-dasharray: 89, 200;
        stroke-dashoffset: -35px;
    }
    100% {
        stroke-dasharray: 89, 200;
        stroke-dashoffset: -124px;
    }
}

@keyframes p-progress-spinner-color {
    100%,
    0% {
        stroke: #d62d20;
    }
    40% {
        stroke: #0057e7;
    }
    66% {
        stroke: #008744;
    }
    80%,
    90% {
        stroke: #ffa700;
    }
}
`,I={spinner:function(c){var t=c.props;return{animationDuration:t.animationDuration}}},x=E.extend({defaultProps:{__TYPE:"ProgressSpinner",id:null,style:null,className:null,strokeWidth:"2",fill:"none",animationDuration:"2s",children:void 0},css:{classes:Y,styles:_,inlineStyles:I}}),j=l.memo(l.forwardRef(function(n,c){var t=R(),o=l.useContext(B),r=x.getProps(n,o),g=l.useRef(null),p=x.setMetaData({props:r}),m=p.ptm,u=p.cx,y=p.sx,h=p.isUnstyled;q(x.css.styles,h,{name:"progressspinner"}),l.useImperativeHandle(c,function(){return{props:r,getElement:function(){return g.current}}});var d=t({id:r.id,ref:g,style:r.style,className:T(r.className,u("root")),role:"progressbar","aria-busy":!0},x.getOtherProps(r),m("root")),k=t({className:u("spinner"),viewBox:"25 25 50 50",style:y("spinner")},m("spinner")),s=t({className:u("circle"),cx:"50",cy:"50",r:"20",fill:r.fill,strokeWidth:r.strokeWidth,strokeMiterlimit:"10"},m("circle"));return l.createElement("div",d,l.createElement("svg",k,l.createElement("circle",s)))}));j.displayName="ProgressSpinner";const z=n=>e.jsx(C,{title:n.title,visible:!0,onCancel:()=>{},buttons:null,children:e.jsxs("div",{className:"flex flex-col items-center justify-center gap-4 py-4",children:[e.jsx(j,{}),e.jsx("p",{className:"m-0 text-center",children:n.message})]})}),L=()=>{const{request:n,closeDialog:c}=O(),t=o=>{c(o)};return e.jsx(C,{title:n.title,visible:!0,onClose:t,buttons:n.buttons,children:e.jsx("p",{className:"m-0",children:n.message})})},$=({title:n,visible:c=!0,onClose:t,buttons:o=f.OkCancel,children:r,width:g="450px",resizable:p=!1,isValid:m,okLabel:u="Ok",cancelLabel:y="Cancel"})=>{const{closeDialog:h}=V(),d=m!==!1,k=e.jsx("div",{className:"inline-flex align-items-center justify-content-center gap-2",children:e.jsx("span",{className:"font-bold white-space-nowrap",children:n})}),s=async v=>{await t(v)!==!1&&h(v)},b=e.jsx(e.Fragment,{children:e.jsx(i,{label:u,icon:"pi pi-check",onClick:()=>s(a.Ok),disabled:!d,autoFocus:!0})}),N=e.jsxs(e.Fragment,{children:[e.jsx(i,{label:u,icon:"pi pi-check",onClick:()=>s(a.Ok),disabled:!d,autoFocus:!0}),e.jsx(i,{label:y,icon:"pi pi-times",outlined:!0,onClick:()=>s(a.Cancelled)})]}),D=e.jsxs(e.Fragment,{children:[e.jsx(i,{label:"Yes",icon:"pi pi-check",onClick:()=>s(a.Yes),disabled:!d,autoFocus:!0}),e.jsx(i,{label:"No",icon:"pi pi-times",outlined:!0,onClick:()=>s(a.No)})]}),P=e.jsxs(e.Fragment,{children:[e.jsx(i,{label:"Yes",icon:"pi pi-check",onClick:()=>s(a.Yes),disabled:!d,autoFocus:!0}),e.jsx(i,{label:"No",icon:"pi pi-times",outlined:!0,onClick:()=>s(a.No)}),e.jsx(i,{label:"Cancel",icon:"pi pi-times",outlined:!0,onClick:()=>s(a.Cancelled)})]}),w=()=>{if(typeof o!="number")return o;switch(o){case f.Ok:return b;case f.OkCancel:return N;case f.YesNo:return D;case f.YesNoCancel:return P}return e.jsx(e.Fragment,{})},F=e.jsx("div",{className:"flex flex-wrap justify-content-start gap-3",children:w()});return e.jsx(S,{header:k,modal:!0,footer:F,onHide:typeof o=="number"?()=>s(a.Cancelled):()=>{},visible:c,style:{width:g},resizable:p,closable:typeof o=="number",children:r})};$.__docgenInfo={description:"",methods:[],displayName:"Dialog",props:{title:{required:!0,tsType:{name:"string"},description:""},visible:{required:!1,tsType:{name:"boolean"},description:"",defaultValue:{value:"true",computed:!1}},onClose:{required:!0,tsType:{name:"signature",type:"function",raw:"(result: DialogResult) => boolean | void | Promise<boolean> | Promise<void>",signature:{arguments:[{type:{name:"DialogResult"},name:"result"}],return:{name:"union",raw:"boolean | void | Promise<boolean> | Promise<void>",elements:[{name:"boolean"},{name:"void"},{name:"Promise",elements:[{name:"boolean"}],raw:"Promise<boolean>"},{name:"Promise",elements:[{name:"void"}],raw:"Promise<void>"}]}}},description:""},buttons:{required:!1,tsType:{name:"union",raw:"DialogButtons | ReactNode",elements:[{name:"DialogButtons"},{name:"ReactNode"}]},description:"",defaultValue:{value:"DialogButtons.OkCancel",computed:!0}},children:{required:!0,tsType:{name:"ReactNode"},description:""},width:{required:!1,tsType:{name:"string"},description:"",defaultValue:{value:"'450px'",computed:!1}},resizable:{required:!1,tsType:{name:"boolean"},description:"",defaultValue:{value:"false",computed:!1}},isValid:{required:!1,tsType:{name:"boolean"},description:""},okLabel:{required:!1,tsType:{name:"string"},description:"",defaultValue:{value:"'Ok'",computed:!1}},cancelLabel:{required:!1,tsType:{name:"string"},description:"",defaultValue:{value:"'Cancel'",computed:!1}}}};export{z as B,L as C,$ as D};
