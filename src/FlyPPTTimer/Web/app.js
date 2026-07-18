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
  renderPresentations(p.presentations||[]);setAvailability(t,p);updatePresentationHint(p);if(s.message)notify(s.message);
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
      if(cmd==='ppt.closeCurrentPresentation')disabled||=!p.isCurrentPresentationManaged;
      if(cmd==='ppt.exitApplication')disabled||=!p.powerPointRunning;
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
    'ppt.closeCurrentPresentation':['关闭当前受控文稿','只会关闭 FlyPPTTimer 以只读方式打开的电脑端文稿。'],
    'ppt.exitApplication':['退出电脑端 PowerPoint','手机遥控网页会保持打开并继续同步；只有没有用户自行打开的文稿时才会退出。'],
    'ppt.forceQuitAll':['强制退出电脑端程序','将强制退出电脑端全部 PowerPoint/WPS/演示软件，未保存内容可能丢失。']
  }[name];
  if(!details)return false;
  pendingConfirmation={name,extra};
  $('confirmTitle').textContent=details[0];$('confirmText').textContent=details[1];$('confirmPanel').hidden=false;$('confirmAccept').focus();
  return true;
}
function closeConfirmation(){pendingConfirmation=null;$('confirmPanel').hidden=true;}
async function command(name,extra={}){
  if(!connected||busy){if(!connected)notify('当前已断开，命令未发送',true);return false}
  if(!extra.confirmed&&requestConfirmation(name,extra))return false;
  busy=true;setAvailability(timerState(lastState||{}),(lastState||{}).presentationState||{});
  try{paint(await api('/command',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({command:name,...extra})}));return true}
  catch(e){notify(e.message,true);return false}finally{busy=false;setAvailability(timerState(lastState||{}),(lastState||{}).presentationState||{});schedulePoll(100)}
}
document.querySelectorAll('.tab').forEach(tab=>tab.addEventListener('click',()=>{document.querySelectorAll('.tab,.page').forEach(x=>x.classList.remove('active'));tab.classList.add('active');$(tab.dataset.page).classList.add('active')}));
$('confirmCancel').addEventListener('click',closeConfirmation);
$('confirmAccept').addEventListener('click',()=>{const pending=pendingConfirmation;closeConfirmation();if(pending)command(pending.name,{...pending.extra,confirmed:true});});
commandButtons.forEach(button=>button.addEventListener('click',()=>command(button.dataset.command)));
$('durationHours').addEventListener('input',()=>timerEditorDirty=true);
$('durationMinutes').addEventListener('input',()=>timerEditorDirty=true);
$('durationSeconds').addEventListener('input',()=>timerEditorDirty=true);
$('applyDuration').addEventListener('click',async()=>{
  const hours=Number($('durationHours').value),minutes=Number($('durationMinutes').value),seconds=Number($('durationSeconds').value);
  if(!Number.isInteger(hours)||hours<0||hours>23||!Number.isInteger(minutes)||minutes<0||minutes>59||!Number.isInteger(seconds)||seconds<0||seconds>59){notify('请输入有效的时、分、秒',true);return}
  const durationMs=(hours*3600+minutes*60+seconds)*1000;
  if(durationMs<=0){notify('计时时长必须大于 0 秒',true);return}
  if(await command('timer.setDuration',{durationMs,presentationId:selectedPresentationId})){timerEditorDirty=false;notify('计时时长已同步到电脑')}
});
timerModeButtons.forEach(button=>button.addEventListener('click',async()=>{if(await command('timer.setMode',{mode:button.dataset.timerMode,presentationId:selectedPresentationId}))notify(`已切换为${button.dataset.timerMode==='countup'?'正计时':'倒计时'}`)}));
$('gotoSlide').addEventListener('click',()=>{const input=$('slideNumber'),value=Number(input.value),max=Number(input.max);if(!Number.isInteger(value)||value<1||value>max){notify(`请输入 1 到 ${max} 之间的页码`,true);input.focus();return}command('ppt.gotoSlide',{slideNumber:value})});
poll();
