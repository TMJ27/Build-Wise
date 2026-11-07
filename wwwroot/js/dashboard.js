'use strict';

// Merge additional example records without replacing anything already edited.
const dashboardCustomers = [
    {id:5,name:'Rudo Mutasa',email:'rudo.mutasa@example.com',phone:'+263 77 456 8290',location:'Avondale, Harare'},
    {id:6,name:'Oakfield Offices',email:'facilities@oakfield.example',phone:'+263 24 278 1420',location:'Alexandra Park, Harare'},
    {id:7,name:'Simba Dube',email:'simba.dube@example.com',phone:'+263 71 829 4400',location:'Marlborough, Harare'},
    {id:8,name:'Chiedza Ncube',email:'chiedza.ncube@example.com',phone:'+263 78 441 2860',location:'Highlands, Harare'},
    {id:9,name:'Pinewood Guesthouse',email:'manager@pinewood.example',phone:'+263 24 244 8120',location:'Chisipite, Harare'},
    {id:10,name:'Farai Chuma',email:'farai.chuma@example.com',phone:'+263 77 911 2034',location:'Madokero, Harare'}
];
const dashboardRequests = [
    {id:1038,title:'Bathroom plumbing upgrade',customer:5,category:'Plumbing',status:'Needs clarification',date:'02 Nov 2026',description:'Replace the bathroom taps and inspect the existing hot-water supply.'},
    {id:1037,title:'Office electrical assessment',customer:6,category:'Electrical',status:'Scope review',date:'02 Nov 2026',description:'Assess the wiring and distribution board before adding office workstations.'},
    {id:1036,title:'Exterior wall painting',customer:7,category:'Painting',status:'Ready to quote',date:'01 Nov 2026',description:'Prepare and repaint the exterior walls of an existing single-storey home.'},
    {id:1035,title:'Kitchen cabinet refurbishment',customer:8,category:'Renovations',status:'Draft quotation',date:'01 Nov 2026',description:'Refurbish cabinet doors and replace handles in the existing kitchen.'},
    {id:1034,title:'Guesthouse maintenance',customer:9,category:'Maintenance',status:'Quotation approved',date:'31 Oct 2026',description:'Carry out agreed minor door, tap and finish repairs across four guest rooms.'},
    {id:1033,title:'Kitchen plumbing repairs',customer:10,category:'Plumbing',status:'Quotation approved',date:'30 Oct 2026',description:'Repair the leaking kitchen sink supply and replace the agreed fittings.'},
    {id:1032,title:'Guest room painting',customer:9,category:'Painting',status:'Quotation approved',date:'29 Oct 2026',description:'Prepare and repaint the walls of two guest rooms.'},
    {id:1031,title:'Office lighting upgrade',customer:6,category:'Electrical',status:'Quotation approved',date:'28 Oct 2026',description:'Replace the agreed light fittings in the reception and meeting room.'}
];
const dashboardQuotes = [
    {id:126,request:1035,status:'Draft',total:1850,revision:1,preparedOn:'2026-11-02'},
    {id:125,request:1034,status:'Approved',total:1260,revision:2,preparedOn:'2026-11-01'},
    {id:124,request:1033,status:'Approved',total:375,revision:1,preparedOn:'2026-10-31'},
    {id:123,request:1032,status:'Approved',total:980,revision:1,preparedOn:'2026-10-30'},
    {id:122,request:1031,status:'Approved',total:1420,revision:1,preparedOn:'2026-10-28'}
];
if (!state.dashboardRecordsAdded) {
    for (const c of dashboardCustomers) if (!state.customers.some(x=>x.id===c.id)) state.customers.push(c);
    for (const r of dashboardRequests) if (!state.requests.some(x=>x.id===r.id)) state.requests.push(r);
    for (const q of dashboardQuotes) if (!state.quotes.some(x=>x.id===q.id)) state.quotes.push(q);
    state.dashboardRecordsAdded = true;
    save();
}

let dashboardRange = '30', dashboardTab = 'requests', dashboardSearch = '';
const dashboardToday = new Date('2026-11-02T12:00:00+02:00');
const dashboardDate = value => {
    if (!value) return new Date('2026-10-31T12:00:00+02:00');
    if (/^\d{4}-\d{2}-\d{2}$/.test(value)) return new Date(value+'T12:00:00+02:00');
    const [day,month,year]=value.split(' ');
    const months=['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
    return new Date(`${year}-${String(months.indexOf(month)+1).padStart(2,'0')}-${day.padStart(2,'0')}T12:00:00+02:00`);
};
const inDashboardRange = date => dashboardRange === 'all' ||
    (dashboardDate(date) >= new Date(dashboardToday.getTime()-(Number(dashboardRange)-1)*86400000) && dashboardDate(date) <= dashboardToday);
const dashboardQuoteDate = q => q.preparedOn || (q.id===127?'2026-10-29':'2026-10-31');

function dashboardRecordsTable(requests,quotes) {
    const term = dashboardSearch.toLowerCase();
    if (dashboardTab === 'requests') {
        const filtered = requests.filter(r=>`${r.title} ${customer(r.customer).name} ${r.id}`.toLowerCase().includes(term));
        return requestTable(filtered.slice(0,6),true)+`<div class="table-foot"><span>Showing ${Math.min(6,filtered.length)} of ${filtered.length} requests</span><a href="#/requests">View all requests →</a></div>`;
    }
    const filtered = quotes.filter(q=>{const r=state.requests.find(r=>r.id===q.request);return r&&`${r.title} ${customer(r.customer).name} ${q.id}`.toLowerCase().includes(term)});
    return `<div class="table-wrap"><table><thead><tr><th>Quotation / customer</th><th>Status</th><th>Value (USD)</th><th></th></tr></thead><tbody>${filtered.slice(0,6).map(q=>{const r=state.requests.find(r=>r.id===q.request);return `<tr class="record" data-href="quote/${q.id}"><td><a href="#/quote/${q.id}"><strong>BW-2026-${String(q.id).padStart(4,'0')}</strong><small>${esc(customer(r.customer).name)} · ${esc(r.title)}</small></a></td><td>${badge(q.status)}</td><td>${money(q.total)}</td><td>${I('chevron')}</td></tr>`}).join('')||'<tr><td colspan="4" class="empty">No matching quotations.</td></tr>'}</tbody></table></div><div class="table-foot"><span>Showing ${Math.min(6,filtered.length)} of ${filtered.length} quotations</span><a href="#/quotations">View all quotations →</a></div>`;
}

dashboard = function() {
    const requests = state.requests.filter(r=>inDashboardRange(r.date)).sort((a,b)=>dashboardDate(b.date)-dashboardDate(a.date)||b.id-a.id);
    const quotes = state.quotes.filter(q=>inDashboardRange(dashboardQuoteDate(q))).sort((a,b)=>dashboardDate(dashboardQuoteDate(b))-dashboardDate(dashboardQuoteDate(a))||b.id-a.id);
    const open = requests.filter(r=>!['Quotation approved','Archived'].includes(r.status));
    const drafts = quotes.filter(q=>q.status==='Draft'), approved = quotes.filter(q=>q.status==='Approved');
    const approvedValue = approved.reduce((n,q)=>n+q.total,0), draftValue=drafts.reduce((n,q)=>n+q.total,0);
    const rangeLabel=dashboardRange==='all'?'All records':`Last ${dashboardRange} days`;
    const stages=[['Needs clarification','Clarify','clarify'],['Scope review','Review','scope'],['Ready to quote','Prepare','quote-edit'],['Draft quotation','Quote','quote-edit']];
    const attention = stages.slice(0,3).map(([status,label,route])=>{const r=open.find(r=>r.status===status);return r?{r,route,title:status==='Needs clarification'?'Confirm the missing details':status==='Scope review'?'Review a proposed scope':'Prepare the next quotation',icon:status==='Needs clarification'?'alert':status==='Scope review'?'check':'file',tone:status==='Needs clarification'?'orange':status==='Scope review'?'violet':''}:null}).filter(Boolean);
    const activity = [
        ...requests.map(r=>({date:dashboardDate(r.date),icon:'file',title:'Request captured',detail:`${customer(r.customer).name} · ${r.title}`,href:`request/${r.id}`})),
        ...quotes.map(q=>({date:dashboardDate(dashboardQuoteDate(q)),icon:q.status==='Approved'?'check':'edit',title:q.status==='Approved'?'Quotation approved':'Quotation drafted',detail:`BW-2026-${String(q.id).padStart(4,'0')} · ${money(q.total)}`,href:`quote/${q.id}`}))
    ].sort((a,b)=>b.date-a.date).slice(0,4);
    const categories=[...new Set(requests.map(r=>r.category))].map(name=>({name,count:requests.filter(r=>r.category===name).length})).sort((a,b)=>b.count-a.count);
    const week=Array.from({length:7},(_,i)=>{const d=new Date(dashboardToday.getTime()-(6-i)*86400000);const day=quotes.filter(q=>dashboardDate(dashboardQuoteDate(q)).toDateString()===d.toDateString());return {label:new Intl.DateTimeFormat('en',{weekday:'short',timeZone:'Africa/Harare'}).format(d),prepared:day.length,approved:day.filter(q=>q.status==='Approved').length}});
    const max=Math.max(1,...week.map(d=>d.prepared));
    return `<div class="v2-greeting"><div><div class="eyebrow">MONDAY, 2 NOVEMBER 2026</div><h1>Good morning, Tawanda<span style="color:#aa6248">.</span></h1><p>${open.length} open requests. ${drafts.length} quotations to finish. Let’s move the work forward.</p></div><div class="actions"><label class="form-search">${I('clock')}<select class="form-control" id="dashboard-range" aria-label="Dashboard date range">${[['7','Last 7 days'],['30','Last 30 days'],['all','All time']].map(([v,t])=>`<option value="${v}" ${dashboardRange===v?'selected':''}>${t}</option>`).join('')}</select></label>${B('New request','new-request','plus')}</div></div>
    <div class="v2-feature-grid"><section class="v2-hero db-hero"><div class="eyebrow">YOUR EXPERTISE. A CLEARER NEXT STEP.</div><h2>Good work starts<br>with a clear scope.</h2><p>${open.filter(r=>r.status==='Needs clarification').length} requests need a little more detail.<br>Pick up the conversation and build from there.</p>${B('Review requests','requests','arrow')}${blueprint}</section><section class="v2-queue"><div class="v2-queue-head"><h2>Needs your attention</h2><span class="badge">${attention.length} next steps</span></div><div class="v2-queue-items">${attention.map(({r,route,title,icon,tone})=>`<a class="attention-item" href="#/${route}/${r.id}"><span class="square ${tone}">${I(icon)}</span><div><h3>${title}</h3><p>${esc(customer(r.customer).name)} · ${esc(r.title)}</p></div><span class="db-next">${I('chevron')}</span></a>`).join('')||'<p class="db-empty">No pending actions in this period.</p>'}</div></section></div>
    <div class="v2-metrics">${[['Open requests',open.length,'file',`${open.filter(r=>r.status==='Needs clarification').length} need clarification`],['Draft quotations',drafts.length,'edit',`${money(draftValue)} in preparation`],['Approved quotations',approved.length,'check',`${money(approvedValue)} approved value`],['Customers',new Set(requests.map(r=>r.customer)).size,'users',`${state.services.filter(s=>s.active).length} active catalogue services`]].map(([label,n,icon,note])=>`<div class="v2-metric"><div class="v2-metric-label">${I(icon)}${label}</div><div class="v2-metric-value"><b>${String(n).padStart(2,'0')}</b></div><p>${note}</p></div>`).join('')}</div>
    <div class="db-analytics"><section class="panel"><div class="panel-head"><div><h2>Quotation activity</h2><p>Prepared and approved over the last 7 days.</p></div><span class="badge">${quotes.length} in selected period</span></div><div class="panel-body"><div class="db-chart" role="img" aria-label="Daily prepared and approved quotation counts">${week.map(d=>`<div class="db-chart-day"><div class="db-chart-bars"><span class="db-chart-bar" style="height:${d.prepared/max*100}%" title="${d.label}: ${d.prepared} prepared">${d.prepared?`<i>${d.prepared}</i>`:''}</span><span class="db-chart-bar approved" style="height:${d.approved/max*100}%" title="${d.label}: ${d.approved} approved">${d.approved?`<i>${d.approved}</i>`:''}</span></div><small>${d.label}</small></div>`).join('')}</div><div class="db-chart-footer"><span><i></i>Prepared</span><span><i class="approved"></i>Approved</span><b>Approved value <strong>${money(approvedValue)}</strong></b></div></div></section>${panel('Requests by service','Where your customers need you.',`<div class="db-category-list">${categories.map((c,i)=>`<div class="db-category"><div><span>${esc(c.name)}</span><b>${c.count} ${c.count===1?'request':'requests'}</b></div><div class="db-progress"><span style="width:${requests.length?c.count/requests.length*100:0}%;background:${['#637d9d','#aa6248','#839a8e','#a394b6','#a5b6c7'][i%5]}"></span></div></div>`).join('')||'<p class="db-empty">No requests in this period.</p>'}</div>`,`<span class="badge">${requests.length} total</span>`)}</div>
    <div class="v2-lower"><div><section class="panel"><div class="panel-head"><div class="db-tabs"><button class="${dashboardTab==='requests'?'active':''}" data-dashboard-tab="requests">Recent requests <span>${requests.length}</span></button><button class="${dashboardTab==='quotes'?'active':''}" data-dashboard-tab="quotes">Quotations <span>${quotes.length}</span></button></div><label class="form-search">${I('search')}<input class="form-control" id="dashboard-search" value="${esc(dashboardSearch)}" placeholder="Find a record…" aria-label="Search dashboard records"></label></div><div id="dashboard-records">${dashboardRecordsTable(requests,quotes)}</div></section>${panel('Your quotation pipeline',`Current requests · ${rangeLabel.toLowerCase()}`,`<div class="v2-pipeline">${stages.map(([s,label,route],i)=>{const rows=requests.filter(r=>r.status===s);return `${i?'<span>→</span>':''}<div><a href="${rows.length?`#/${route}/${rows[0].id}`:'#/requests'}"><b>${String(rows.length).padStart(2,'0')}</b><span>${label}</span></a></div>`}).join('')}</div><div class="v2-note">Your approval turns a draft into a commitment. <a href="#/quotations">Review quotations →</a></div>`)}</div><aside>${panel('Latest activity','A little progress, recorded.',activity.map(a=>`<a class="activity" href="#/${a.href}"><span class="avatar">${I(a.icon)}</span><div><p><b>${a.title}</b><br>${esc(a.detail)}</p><small>${new Intl.DateTimeFormat('en-GB',{day:'numeric',month:'short',timeZone:'Africa/Harare'}).format(a.date)}</small></div></a>`).join(''))}${panel('Ready for the next conversation?','',`<p class="db-help-text">Your catalogue has ${state.services.filter(s=>s.active).length} services ready to bring clarity to the next customer request.</p>${B('Open service catalogue','catalogue','box','soft small')}`)}</aside></div>`;
};

document.addEventListener('change',e=>{if(e.target.id==='dashboard-range'){dashboardRange=e.target.value;render()}});
document.addEventListener('click',e=>{const tab=e.target.closest('[data-dashboard-tab]');if(tab){dashboardTab=tab.dataset.dashboardTab;dashboardSearch='';const scroll=window.scrollY;render();window.scrollTo(0,scroll)}});
document.addEventListener('input',e=>{if(e.target.id==='dashboard-search'){dashboardSearch=e.target.value;const requests=state.requests.filter(r=>inDashboardRange(r.date)).sort((a,b)=>dashboardDate(b.date)-dashboardDate(a.date)||b.id-a.id);const quotes=state.quotes.filter(q=>inDashboardRange(dashboardQuoteDate(q))).sort((a,b)=>dashboardDate(dashboardQuoteDate(b))-dashboardDate(dashboardQuoteDate(a))||b.id-a.id);document.querySelector('#dashboard-records').innerHTML=dashboardRecordsTable(requests,quotes)}});
render();

