const token=window.FLYPPT_TOKEN||'';
const $=id=>document.getElementById(id);
const commandButtons=[...document.querySelectorAll('[data-command]')];
let connected=false,lastState=null,messageTimer=null,pollTimer=null,busy=false;

function url(path){return path+(path.includes('?')?'&':'?')+'token='+encodeURIComponent(token)}
async function api(path,options={}){
  const controller=new AbortController(),timeout=setTimeout(()=>controller.abort(),20000);
  try{
    const response=await fetch(url(path),{cache:'no-store',credentials:'omit',...options,signal:controller.signal});
    const data=await response.json();
    if(!response.ok||data.ok===false)throw new Error(data.message||data.error||'操作失败');
    return data;
  }finally{clearTimeout(timeout)}
}
function notify(text,error=false){const el=$('message');el.textContent=text;el.className='message show'+(error?' error':'');clearTimeout(messageTimer);messageTimer=setTimeout(()=>el.className='message',3200)}
function connection(ok){connected=ok;const el=$('connection');el.textContent=ok?'已连接':'已断开';el.className='status '+(ok?'connected':'disconnected');$('syncText').textContent=ok?'最后同步 '+new Date().toLocaleTimeString('zh-CN',{hour12:false}):'连接失败；使用 Clash 时请将局域网地址设为 DIRECT'}
function timerState(s){return s.timerState||s}
function paint(s){
  lastState=s;connection(true);const t=timerState(s),p=s.presentationState||{};
  $('timerText').textContent=t.displayText||'--:--';$('timerStatus').textContent=t.state||'停止';$('timerMode').textContent=t.mode||'倒计时';
  $('pptName').textContent=p.presentationName||(p.powerPointRunning?'未打开演示文稿':'未检测到 PowerPoint');$('pptPath').textContent=p.presentationPath||'';$('pptPath').title=p.presentationPath||'';
  $('pptShowState').textContent=p.isSlideShowRunning?'正在放映':'未放映';$('pptSlide').textContent=`${p.currentSlide||0} / ${p.totalSlides||0}`;$('pptScreen').textContent=p.screenMode||'正常';
  $('blackScreenButton').classList.toggle('selected',p.screenMode==='黑屏');$('whiteScreenButton').classList.toggle('selected',p.screenMode==='白屏');
  renderPresentations(p.presentations||[]);setAvailability(t,p);updatePresentationHint(p);if(s.message)notify(s.message);
}
function renderPresentations(items){
  const host=$('presentationList'),signature=items.map(x=>[x.id,x.name,x.directory,x.isActive,x.isOpen].join(':')).join('|');
  if(host.dataset.signature!==signature){
    host.dataset.signature=signature;host.innerHTML='';
    if(!items.length)host.innerHTML='<div class="hint">没有已打开或文件规则中已启用的演示文稿</div>';
    items.forEach(item=>{
      const row=document.createElement('div');row.className='presentation-item';row.dataset.active=String(!!item.isActive);row.dataset.open=String(!!item.isOpen);
      const text=document.createElement('div'),title=document.createElement('strong'),meta=document.createElement('span');
      title.textContent=item.name;title.title=[item.name,item.directory].filter(Boolean).join('\n');
      meta.textContent=(item.directory?item.directory+' · ':'')+(item.isActive?'当前活动':item.isOpen?'已打开':'文件规则');meta.title=item.directory||'';
      text.append(title,meta);
      const button=document.createElement('button');button.dataset.presentationButton='true';button.textContent=item.isActive?'当前':item.isOpen?'切换':'打开';button.addEventListener('click',()=>command('ppt.openPresentation',{presentationId:item.id}));
      row.append(text,button);host.append(row);
    });
  }
  refreshPresentationButtons();
}
function refreshPresentationButtons(){
  document.querySelectorAll('[data-presentation-button]').forEach(button=>{
    const row=button.closest('.presentation-item'),active=row?.dataset.active==='true';button.disabled=busy||!connected||active;
  });
}
function setAvailability(t,p){
  const show=!!p.isSlideShowRunning,has=!!p.hasPresentation,state=t.state||'',running=!!t.running,paused=state.includes('暂停'),stopped=!running&&!paused;
  commandButtons.forEach(button=>{
    const cmd=button.dataset.command;let disabled=busy||!connected;
    if(cmd==='timer.start')disabled||=!stopped;
    else if(cmd==='timer.pause')disabled||=!running;
    else if(cmd==='timer.resume')disabled||=!paused;
    else if(cmd.startsWith('ppt.')){
      if(['ppt.previous','ppt.next','ppt.gotoSlide','ppt.endShow','ppt.blackScreenToggle','ppt.whiteScreenToggle'].includes(cmd))disabled||=!show;
      if(['ppt.startFromBeginning','ppt.startFromCurrent'].includes(cmd))disabled||=!has;
    }
    button.disabled=disabled;
  });
  const max=Math.max(1,Number(p.totalSlides)||1);$('slideNumber').max=String(max);$('slideNumber').disabled=busy||!connected||!show;$('gotoSlide').disabled=busy||!connected||!show;
  refreshPresentationButtons();
}
function updatePresentationHint(p){
  const updated=p.updatedAt?new Date(p.updatedAt).getTime():0,stale=updated>0&&Date.now()-updated>3000;
  $('presentationHint').textContent=p.error||(!p.powerPointInstalled?'本机未安装 Microsoft PowerPoint':!p.hasPresentation?'请先打开或从上方列表选择演示文稿':stale?'PowerPoint 状态可能已过期，计时控制仍可使用':'');
}
function schedulePoll(delay=1000){clearTimeout(pollTimer);pollTimer=setTimeout(poll,delay)}
async function poll(){
  if(busy){schedulePoll(300);return}
  try{paint(await api('/state'));schedulePoll(1000)}
  catch(e){connection(false);setAvailability({},{});refreshPresentationButtons();notify('连接失败：'+e.message+'。Clash/TUN 请将本机局域网 IP 和端口设为 DIRECT。',true);schedulePoll(2500)}
}
async function command(name,extra={}){
  if(!connected||busy){if(!connected)notify('当前已断开，命令未发送',true);return}
  if(name==='ppt.endShow'&&!confirm('确定结束当前 PowerPoint 放映吗？'))return;
  busy=true;setAvailability(timerState(lastState||{}),(lastState||{}).presentationState||{});
  try{paint(await api('/command',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({command:name,...extra})}))}
  catch(e){notify(e.message,true)}finally{busy=false;setAvailability(timerState(lastState||{}),(lastState||{}).presentationState||{});schedulePoll(100)}
}
document.querySelectorAll('.tab').forEach(tab=>tab.addEventListener('click',()=>{document.querySelectorAll('.tab,.page').forEach(x=>x.classList.remove('active'));tab.classList.add('active');$(tab.dataset.page).classList.add('active')}));
commandButtons.forEach(button=>button.addEventListener('click',()=>command(button.dataset.command)));
$('gotoSlide').addEventListener('click',()=>{const input=$('slideNumber'),value=Number(input.value),max=Number(input.max);if(!Number.isInteger(value)||value<1||value>max){notify(`请输入 1 到 ${max} 之间的页码`,true);input.focus();return}command('ppt.gotoSlide',{slideNumber:value})});
poll();
