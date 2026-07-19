const token=window.FLYPPT_TOKEN||'';
const $=id=>document.getElementById(id);
const commandButtons=[...document.querySelectorAll('[data-command]')];
const timerModeButtons=[...document.querySelectorAll('[data-timer-mode]')];
let connected=false,lastState=null,messageTimer=null,pollTimer=null,busy=false,pendingConfirmation=null,timerEditorDirty=false,selectedPresentationId=null;

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
function setDurationEditor(durationMs){
  const total=Math.max(1,Math.round((Number(durationMs)||0)/1000));
  $('durationHours').value=String(Math.min(23,Math.floor(total/3600)));
  $('durationMinutes').value=String(Math.floor(total%3600/60));
  $('durationSeconds').value=String(total%60);
}
function paint(s){
  lastState=s;connection(true);const t=timerState(s),p=s.presentationState||{};
  $('timerText').textContent=t.displayText||'--:--';$('timerStatus').textContent=t.isOvertime?'已超时':(t.state||'停止');$('timerMode').textContent=t.mode||'倒计时';
  const muted=!!t.muted;$('muteButton').textContent=muted?'电脑已静音（点击恢复声音）':'电脑声音正常（点击静音）';$('muteButton').classList.toggle('selected',muted);$('muteButton').setAttribute('aria-pressed',String(muted));
  document.querySelector('.timer-card').classList.toggle('overtime',!!t.isOvertime);
  const timeUpActive=!!t.timeUpBlackoutActive;
  ['timerDismissTimeUp','pptDismissTimeUp'].forEach(id=>$(id).textContent=timeUpActive?'退出“时间到”黑屏':'当前无“时间到”黑屏');
  if(!timerEditorDirty)setDurationEditor(t.durationMs);
  timerModeButtons.forEach(button=>button.classList.toggle('selected',button.dataset.timerMode===(t.mode==='正计时'?'countup':'countdown')));
  $('pptName').textContent=p.presentationName||(p.powerPointRunning?'未打开演示文稿':'未检测到 PowerPoint');$('pptPath').textContent=p.presentationPath||'';$('pptPath').title=p.presentationPath||'';
  $('pptShowState').textContent=p.isSlideShowRunning?'正在放映':'未放映';$('pptSlide').textContent=`${p.currentSlide||0} / ${p.totalSlides||0}`;$('pptScreen').textContent=p.screenMode||'正常';
  $('blackScreenButton').classList.toggle('selected',p.screenMode==='黑屏');$('whiteScreenButton').classList.toggle('selected',p.screenMode==='白屏');
  renderPresentations(p.presentations||[]);setAvailability(t,p);updatePresentationHint(p);requestAnimationFrame(syncViewportHeight);if(s.message)notify(s.message);
}
function renderPresentations(items){
  const activeItem=items.find(x=>x.isActive);
  if(activeItem)selectedPresentationId=activeItem.id;
  else if(selectedPresentationId&&!items.some(x=>x.id===selectedPresentationId))selectedPresentationId=null;
  const host=$('presentationList'),signature=items.map(x=>[x.id,x.name,x.directory,x.isActive,x.isOpen,x.isManaged].join(':')).join('|');
  if(host.dataset.signature!==signature){
    host.dataset.signature=signature;host.innerHTML='';
    if(!items.length)host.innerHTML='<div class="hint">没有已打开或文件规则中已启用的演示文稿</div>';
    items.forEach(item=>{
      const row=document.createElement('div');row.className='presentation-item';row.dataset.active=String(!!item.isActive);row.dataset.open=String(!!item.isOpen);
      const text=document.createElement('div'),title=document.createElement('strong'),meta=document.createElement('span');
      title.textContent=item.name;title.title=[item.name,item.directory].filter(Boolean).join('\n');
      meta.textContent=(item.directory?item.directory+' · ':'')+(item.isActive?'当前活动':item.isOpen?'已打开':'文件规则');meta.title=item.directory||'';
      text.append(title,meta);
      const button=document.createElement('button');button.dataset.presentationButton='true';button.textContent=item.isActive?'当前':item.isOpen?'切换':'打开';button.addEventListener('click',()=>{selectedPresentationId=item.id;command('ppt.openPresentation',{presentationId:item.id})});
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
  const show=!!p.isSlideShowRunning,has=!!p.hasPresentation,state=t.state||'',running=!!t.running,paused=state.includes('暂停'),stopped=!running&&!paused,operationBusy=!!p.isOperationBusy;
  commandButtons.forEach(button=>{
    const cmd=button.dataset.command;let disabled=busy||operationBusy||!connected;
    if(cmd==='timer.start')disabled||=!stopped;
    else if(cmd==='timeup.dismiss')disabled||=!t.timeUpBlackoutActive;
    else if(cmd==='timer.pause')disabled||=!running;
    else if(cmd==='timer.resume')disabled||=!paused;
    else if(cmd.startsWith('ppt.')){
      if(['ppt.previous','ppt.next','ppt.gotoSlide','ppt.endShow','ppt.blackScreenToggle','ppt.whiteScreenToggle'].includes(cmd))disabled||=!show;
      if(['ppt.startFromBeginning','ppt.startFromCurrent'].includes(cmd))disabled||=!has;
      if(cmd==='ppt.closeCurrentPresentation')disabled||=!(Number(p.openPresentationCount)>0||(p.presentations||[]).some(x=>x.isOpen));
    }
    button.disabled=disabled;
  });
  const max=Math.max(1,Number(p.totalSlides)||1);$('slideNumber').max=String(max);$('slideNumber').disabled=busy||!connected||!show;$('gotoSlide').disabled=busy||!connected||!show;
  $('applyDuration').disabled=busy||!connected;
  timerModeButtons.forEach(button=>button.disabled=busy||!connected);
  refreshPresentationButtons();
}
function updatePresentationHint(p){
  const updated=p.updatedAt?new Date(p.updatedAt).getTime():0,stale=updated>0&&Date.now()-updated>3000;
  $('presentationHint').textContent=p.error||(!p.powerPointInstalled?'本机未安装 Microsoft PowerPoint':!p.hasPresentation?'请先打开或从上方列表选择演示文稿':stale?'PowerPoint 状态可能已过期，计时控制仍可使用':'');
}
function schedulePoll(delay=1000){clearTimeout(pollTimer);pollTimer=setTimeout(poll,delay)}
async function poll(){
  try{paint(await api('/state'));schedulePoll(1000)}
  catch(e){connection(false);setAvailability({},{});refreshPresentationButtons();notify('连接失败：'+e.message+'。Clash/TUN 请将本机局域网 IP 和端口设为 DIRECT。',true);schedulePoll(2500)}
}
function requestConfirmation(name,extra){
  const details={
    'ppt.endShow':['结束放映','仅结束电脑端当前放映，不关闭文稿或 PowerPoint。'],
    'ppt.closeCurrentPresentation':['关闭最后打开的文稿','将按打开顺序关闭最后打开的文稿且不保存；每次只关闭一个。'],
    'ppt.forceQuitAll':['退出演示软件','将强制退出电脑端全部 PowerPoint/WPS/演示软件，未保存内容可能丢失。']
  }[name];
  if(!details)return false;
  pendingConfirmation={name,extra};
  $('confirmTitle').textContent=details[0];$('confirmText').textContent=details[1];$('confirmPanel').hidden=false;$('confirmAccept').focus();
  return true;
}
function closeConfirmation(){pendingConfirmation=null;$('confirmPanel').hidden=true;$('confirmCancel').textContent='取消';$('confirmAccept').textContent='确认';$('confirmAccept').classList.add('danger')}
function requestDurationConfirmation(durationMs,ruleCount){
  pendingConfirmation={name:'timer.setDuration',extra:{durationMs},durationChoice:true};
  $('confirmTitle').textContent='同步文件规则时长？';$('confirmText').textContent=`当前已有 ${ruleCount} 个待控演示文稿。是否把新时长同步应用到全部文件规则？`;
  $('confirmCancel').textContent='仅修改全局';$('confirmAccept').textContent='同步全部';$('confirmAccept').classList.remove('danger');$('confirmPanel').hidden=false;$('confirmAccept').focus();
}
async function command(name,extra={}){
  if(!connected||busy){if(!connected)notify('当前已断开，命令未发送',true);return false}
  if(!extra.confirmed&&requestConfirmation(name,extra))return false;
  busy=true;setAvailability(timerState(lastState||{}),(lastState||{}).presentationState||{});
  try{paint(await api('/command',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({command:name,...extra})}));return true}
  catch(e){notify(e.message,true);return false}finally{busy=false;setAvailability(timerState(lastState||{}),(lastState||{}).presentationState||{});schedulePoll(100)}
}
const pages=['timerPage','pptPage'],pagesTrack=$('pagesTrack'),pagesViewport=$('pagesViewport');
const SWIPE_DIRECTION_DISTANCE=10,SWIPE_MAX_ANGLE_DEGREES=35;
let pageIndex=0,swipeStart=null,suppressSwipeClick=false,suppressSwipeClickTimer=null;
function syncViewportHeight(){const page=$(pages[pageIndex]);if(page)pagesViewport.style.height=`${page.scrollHeight}px`}
function readTrackX(){const transform=getComputedStyle(pagesTrack).transform;if(!transform||transform==='none')return-pageIndex*pagesViewport.clientWidth;try{return new DOMMatrixReadOnly(transform).m41}catch{return Number(transform.match(/matrix\([^,]+,[^,]+,[^,]+,[^,]+,\s*([^,]+)/)?.[1])||0}}
function freezeTrack(){const x=readTrackX();pagesTrack.style.transition='none';pagesTrack.style.transform=`translate3d(${x}px,0,0)`;pagesTrack.getBoundingClientRect();return x}
function renderPage(index,animate=true){
  const target=Math.max(0,Math.min(pages.length-1,index)),width=Math.max(1,pagesViewport.clientWidth);if(animate)freezeTrack();pageIndex=target;pagesTrack.style.transition=animate?'transform 280ms cubic-bezier(.22,.75,.25,1)':'none';pagesTrack.style.transform=`translate3d(${-pageIndex*width}px,0,0)`;
  document.querySelectorAll('.tab,.page').forEach(x=>x.classList.remove('active'));document.querySelector(`.tab[data-page="${pages[pageIndex]}"]`)?.classList.add('active');$(pages[pageIndex]).classList.add('active');requestAnimationFrame(syncViewportHeight)
}
function activatePage(pageId){renderPage(Math.max(0,pages.indexOf(pageId)),true)}
document.querySelectorAll('.tab').forEach(tab=>tab.addEventListener('click',()=>activatePage(tab.dataset.page)));
pagesViewport.addEventListener('touchstart',event=>{if(event.touches.length!==1||event.target.closest('.confirm-panel'))return;const touch=event.touches[0];swipeStart={x:touch.clientX,y:touch.clientY,time:performance.now(),baseX:freezeTrack(),dragging:false,lastX:touch.clientX,lastTime:performance.now()}},{passive:true});
pagesViewport.addEventListener('touchmove',event=>{if(!swipeStart||!event.touches.length)return;const touch=event.touches[0],dx=touch.clientX-swipeStart.x,dy=touch.clientY-swipeStart.y;if(!swipeStart.dragging){const distance=Math.hypot(dx,dy);if(distance<SWIPE_DIRECTION_DISTANCE)return;const angle=Math.atan2(Math.abs(dy),Math.abs(dx))*180/Math.PI;if(angle>SWIPE_MAX_ANGLE_DEGREES){swipeStart=null;renderPage(pageIndex,true);return}swipeStart.dragging=true;suppressSwipeClick=true;clearTimeout(suppressSwipeClickTimer)}event.preventDefault();const width=Math.max(1,pagesViewport.clientWidth),minX=-(pages.length-1)*width;let nextX=swipeStart.baseX+dx;if(nextX>0)nextX*=.24;else if(nextX<minX)nextX=minX+(nextX-minX)*.24;pagesTrack.style.transition='none';pagesTrack.style.transform=`translate3d(${nextX}px,0,0)`;swipeStart.lastX=touch.clientX;swipeStart.lastTime=performance.now()},{passive:false});
pagesViewport.addEventListener('touchend',event=>{if(!swipeStart)return;const touch=event.changedTouches[0],now=performance.now(),x=touch?touch.clientX:swipeStart.lastX,dx=x-swipeStart.x,elapsed=Math.max(1,now-swipeStart.time),dragging=swipeStart.dragging,currentX=readTrackX(),width=Math.max(1,pagesViewport.clientWidth);swipeStart=null;if(!dragging){renderPage(pageIndex,true);return}const velocity=dx/elapsed,commit=Math.abs(dx)>=18||Math.abs(velocity)>=.12,position=-currentX/width;let target=Math.round(position);if(commit)target=dx<0?Math.floor(position)+1:Math.ceil(position)-1;renderPage(target,true);suppressSwipeClickTimer=setTimeout(()=>suppressSwipeClick=false,420)},{passive:true});
pagesViewport.addEventListener('touchcancel',()=>{swipeStart=null;renderPage(pageIndex,true)},{passive:true});
document.addEventListener('click',event=>{if(!suppressSwipeClick)return;suppressSwipeClick=false;clearTimeout(suppressSwipeClickTimer);event.preventDefault();event.stopPropagation() },true);
pagesTrack.addEventListener('transitionend',event=>{if(event.propertyName==='transform')syncViewportHeight()});
window.addEventListener('resize',()=>{renderPage(pageIndex,false);requestAnimationFrame(syncViewportHeight)});
renderPage(0,false);
$('confirmCancel').addEventListener('click',()=>{const pending=pendingConfirmation;if(pending?.durationChoice){closeConfirmation();command(pending.name,{...pending.extra,syncAllRules:false,confirmed:true}).then(ok=>{if(ok){timerEditorDirty=false;notify('已修改全局时长，文件规则保持不变')}})}else closeConfirmation()});
$('confirmAccept').addEventListener('click',()=>{const pending=pendingConfirmation;closeConfirmation();if(pending)command(pending.name,{...pending.extra,...(pending.durationChoice?{syncAllRules:true}:{}),confirmed:true}).then(ok=>{if(ok&&pending.durationChoice){timerEditorDirty=false;notify('已同步修改全部文件规则时长')}})});
commandButtons.forEach(button=>button.addEventListener('click',()=>command(button.dataset.command)));
$('durationHours').addEventListener('input',()=>timerEditorDirty=true);
$('durationMinutes').addEventListener('input',()=>timerEditorDirty=true);
$('durationSeconds').addEventListener('input',()=>timerEditorDirty=true);
$('applyDuration').addEventListener('click',async()=>{
  const hours=Number($('durationHours').value),minutes=Number($('durationMinutes').value),seconds=Number($('durationSeconds').value);
  if(!Number.isInteger(hours)||hours<0||hours>23||!Number.isInteger(minutes)||minutes<0||minutes>59||!Number.isInteger(seconds)||seconds<0||seconds>59){notify('请输入有效的时、分、秒',true);return}
  const durationMs=(hours*3600+minutes*60+seconds)*1000;
  if(durationMs<=0){notify('计时时长必须大于 0 秒',true);return}
  const ruleCount=Number(timerState(lastState||{}).ruleCount)||0;
  if(ruleCount>0){requestDurationConfirmation(durationMs,ruleCount);return}
  if(await command('timer.setDuration',{durationMs,syncAllRules:false})){timerEditorDirty=false;notify('计时时长已同步到电脑')}
});
timerModeButtons.forEach(button=>button.addEventListener('click',async()=>{if(await command('timer.setMode',{mode:button.dataset.timerMode,presentationId:selectedPresentationId}))notify(`已切换为${button.dataset.timerMode==='countup'?'正计时':'倒计时'}`)}));
$('gotoSlide').addEventListener('click',()=>{const input=$('slideNumber'),value=Number(input.value),max=Number(input.max);if(!Number.isInteger(value)||value<1||value>max){notify(`请输入 1 到 ${max} 之间的页码`,true);input.focus();return}command('ppt.gotoSlide',{slideNumber:value})});
poll();
