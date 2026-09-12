const effectiveLanguage=navigator.language.toLowerCase().startsWith('zh')?'zh-CN':'en';
document.documentElement.lang=effectiveLanguage;
const webEnglish={
  "界面主题":"Interface theme", "跟随电脑":"Follow computer", "浅色":"Light", "深色":"Dark",
  "已保存":"Saved", "有未保存的时长修改":"Unsaved duration changes",
  "排序方式":"Sort by",
  "手动排序":"Manual order",
  "按名称排序":"Name",
  "按文件大小排序":"File size",
  "按修改时间排序":"Date modified",
  "升序":"Ascending",
  "降序":"Descending",
  "长按文件或拖动手柄调整顺序":"Long-press a file or its drag handle to reorder",
  "受控文件列表":"Controlled files",
  "暂无受控文件":"No controlled files",
  "添加已打开文件":"Add an open file",
  "已打开但未受控的文件":"Open files not in the controlled list",
  "移除文件":"Remove file",
  "长按拖动排序":"Long-press to drag; arrow keys also reorder",
  "顺序已保存":"Order saved",
  "没有可添加的已打开文件":"No open files to add",
  "所有文件已隐藏":"All files are hidden",
  "移除后不再显示在受控列表，也不能远程打开或关闭。不删除磁盘文件，不关闭当前文稿；需要时可重新加入。":"Removes the file from remote control. It can no longer be opened or closed remotely until added again. The disk file and any open document are kept.",
  "当前文稿未受控，请先加入列表。":"The current presentation is not controlled. Add it to the list first.",
  "受控文件":"Controlled file",

  '隐藏':'Hide','恢复':'Restore','显示隐藏项':'Show hidden items','收起隐藏项':'Hide hidden items',
  '移除规则':'Remove rule','加入列表':'Add to list','上移':'Move up','下移':'Move down',
  '列表已更新':'List updated','删除列表规则':'Remove list rule',
  '只移除 FlyPPTTimer 的列表规则，不删除磁盘文件，也不关闭已打开文稿。':'Only removes the FlyPPTTimer list rule. The disk file and open presentation are kept.',

  'FlyPPTTimer 遥控':'FlyPPTTimer Remote','演讲遥控':'Presentation Remote','正在连接电脑...':'Connecting to computer...',
  '连接中':'Connecting','遥控页面':'Remote pages','计时':'Timer','演示':'Presentation','等待同步':'Waiting for sync',
  '倒计时':'Countdown','正计时':'Count up','计时设置（同步到电脑）':'Timer settings (sync to computer)',
  '计时时长':'Timer duration','时':'Hours','分':'Minutes','秒':'Seconds','应用时长':'Apply duration','计时模式':'Timer mode',
  '修改后会立即保存到电脑端设置。':'Changes are saved to the computer immediately.','开始':'Start','暂停':'Pause','继续':'Resume',
  '停止并重置':'Stop & reset','重新计时':'Restart timer','显示 / 隐藏':'Show / Hide','触发闪烁':'Flash','电脑声音正常（点击静音）':'Computer audio on (tap to mute)',
  '电脑已静音（点击恢复声音）':'Computer muted (tap to unmute)','当前无“时间到”黑屏':'No “Time\'s up” screen',
  '退出“时间到”黑屏':'Dismiss “Time\'s up” screen','当前演示':'Current presentation','未检测到 PowerPoint':'PowerPoint not detected',
  '请先打开演示文稿':'Open a presentation first','未放映':'Not presenting','正常':'Normal','演示文稿列表':'Presentations',
  '上一页':'Previous','下一页':'Next','从头放映':'Start from beginning','从当前页放映':'Start from current slide','页码':'Slide',
  '跳转':'Go','黑屏 / 恢复':'Black screen / Restore','白屏 / 恢复':'White screen / Restore','结束放映':'End slide show',
  '关闭当前文稿':'Close current presentation','关闭最后打开的文稿':'Close last-opened presentation','退出演示软件':'Quit presentation software','确认操作':'Confirm action',
  '取消':'Cancel','确认':'Confirm','正在建立连接...':'Connecting...','已连接':'Connected','已断开':'Disconnected',
  '已超时':'Overtime','停止':'Stopped','正在放映':'Presenting','黑屏':'Black screen','白屏':'White screen',
  '未打开演示文稿':'No presentation is open','没有已打开或文件规则中已启用的演示文稿':'No open presentations or enabled presentation rules',
  '当前活动':'Active','已打开':'Open','文件规则':'Rule','当前':'Current','切换':'Switch','打开':'Open',
  '本机未安装 Microsoft PowerPoint':'Microsoft PowerPoint is not installed','请先打开或从上方列表选择演示文稿':'Open or select a presentation above',
  'PowerPoint 状态可能已过期，计时控制仍可使用':'PowerPoint status may be stale; timer controls are still available',
  '仅结束电脑端当前放映，不关闭文稿或 PowerPoint。':'Ends the current slide show without closing the presentation or PowerPoint.',
  '关闭当前活动文稿且不保存；其他已打开文稿保持不变。':'Closes the active presentation without saving. Other open presentations remain open.',
  '将按打开顺序关闭最后打开的文稿且不保存；每次只关闭一个。':'Closes the last-opened presentation without saving, one at a time.',
  '将强制退出电脑端全部 PowerPoint/WPS/演示软件，未保存内容可能丢失。':'Force-quits all PowerPoint/WPS presentation apps. Unsaved work may be lost.',
  '同步文件规则时长？':'Sync presentation-rule durations?','仅修改全局':'Global only','同步全部':'Sync all',
  '当前已断开，命令未发送':'Disconnected; command was not sent','请输入有效的时、分、秒':'Enter valid hours, minutes, and seconds',
  '计时时长必须大于 0 秒':'Timer duration must be greater than 0 seconds','计时时长已同步到电脑':'Timer duration synced to computer'
  ,'操作失败':'Operation failed','简体中文':'Simplified Chinese',
  '连接失败；使用 Clash 时请将局域网地址设为 DIRECT':'Connection failed. If you use Clash, route the LAN address directly.',
  '已同步修改全部文件规则时长':'All presentation-rule durations updated',
  '已修改全局时长，文件规则保持不变':'Global duration changed; presentation rules unchanged',
  '运行中':'Running','已结束':'Finished','电脑已静音':'Computer muted','电脑声音已恢复':'Computer audio restored',
  '已退出“时间到”黑屏':'Dismissed the “Time\'s up” screen','白屏':'White screen','黑屏':'Black screen',
  '演示控制服务当前不可用。':'Presentation control is unavailable.','演示控制服务已关闭。':'Presentation control is disabled.',
  '命令不在演示控制白名单中。':'Command is not in the presentation-control allowlist.',
  '强制退出会丢失所有未保存内容，请再次确认。':'Force quit discards all unsaved work. Confirm again.',
  '演示操作正在进行，请等待当前操作完成。':'A presentation operation is in progress. Wait for it to finish.',
  '演示命令队列繁忙，请稍后重试。':'The presentation command queue is busy. Try again shortly.',
  'PowerPoint 响应超时，计时遥控仍可继续使用。':'PowerPoint timed out. Timer remote control is still available.',
  '请输入有效页码。':'Enter a valid slide number.','请先选择演示文稿。':'Select a presentation first.',
  '演示文稿文件不存在。':'The presentation file does not exist.','未安装 Microsoft PowerPoint。':'Microsoft PowerPoint is not installed.',
  '当前没有正在运行的 PowerPoint 放映。':'No PowerPoint slide show is running.',
  '当前没有可关闭的演示文稿。':'There is no presentation to close.',
  '正在打开演示文稿':'Opening presentation','正在启动 PowerPoint':'Starting PowerPoint',
  '正在启动放映':'Starting slide show','正在结束放映':'Ending slide show',
  '正在关闭最后打开的文稿':'Closing the last-opened presentation','正在强制退出演示程序':'Force-quitting presentation software',
  '正在关闭当前文稿':'Closing the current presentation',
  '已从头开始放映':'Slide show started from the beginning','已结束放映':'Slide show ended',
  '已切换到上一页':'Moved to the previous slide','已切换到下一页':'Moved to the next slide',
  '状态已刷新':'Status refreshed'
};
function wt(text){
  if(effectiveLanguage!=='en'||!text)return text;
  if(webEnglish[text])return webEnglish[text];
  return text
    .replace(/^最后同步 /,'Last synced ')
    .replace(/^连接失败：/,'Connection failed: ')
    .replace(/^请输入 1 到 (\d+) 之间的页码$/,'Enter a slide number from 1 to $1')
    .replace(/^当前已有 (\d+) 个待控演示文稿。是否把新时长同步应用到全部文件规则？$/,'There are $1 managed presentations. Apply the new duration to every rule?')
    .replace(/^已切换为/,'Changed to ')
    .replace(/^已从第 (\d+) 页开始放映$/,'Slide show started from slide $1')
    .replace(/^已跳转到第 (\d+) 页$/,'Moved to slide $1')
    .replace(/^已打开 (.+)$/,'Opened $1')
    .replace(/^已关闭最后打开的文稿：(.+)。$/,'Closed the last-opened presentation: $1.')
    .replace(/^已关闭当前文稿：(.+)。$/,'Closed the current presentation: $1.')
    .replace(/^已修改全局时长，文件规则保持不变$/,'Global duration changed; presentation rules unchanged')
    .replace(/^已同步修改全部文件规则时长$/,'All presentation-rule durations updated');
}
function translateWeb(root=document){
  if(effectiveLanguage!=='en')return;
  const walker=document.createTreeWalker(root,NodeFilter.SHOW_TEXT);let node;
  while((node=walker.nextNode())){const value=node.nodeValue,trim=value.trim();if(trim&&wt(trim)!==trim)node.nodeValue=value.replace(trim,wt(trim))}
  root.querySelectorAll?.('[title],[placeholder],[aria-label]').forEach(el=>{
    ['title','placeholder','aria-label'].forEach(name=>{if(el.hasAttribute(name))el.setAttribute(name,wt(el.getAttribute(name)))});
  });
  document.title=wt(document.title);
}
translateWeb();
new MutationObserver(records=>records.forEach(record=>{
  if(record.type==='characterData'){const next=wt(record.target.nodeValue);if(next!==record.target.nodeValue)record.target.nodeValue=next}
  record.addedNodes.forEach(node=>{if(node.nodeType===Node.ELEMENT_NODE)translateWeb(node);else if(node.nodeType===Node.TEXT_NODE){const next=wt(node.nodeValue);if(next!==node.nodeValue)node.nodeValue=next}});
})).observe(document.body,{subtree:true,childList:true,characterData:true});
const token=window.FLYPPT_TOKEN||'';
const $=id=>document.getElementById(id);
const commandButtons=[...document.querySelectorAll('[data-command]')];
const timerModeButtons=[...document.querySelectorAll('[data-timer-mode]')];
let stateEpoch=0,lastPaintedRevision=-1,lastServerInstance=null,pollSequence=0,lastPaintedPoll=0;
let connected=false,lastState=null,messageTimer=null,pollTimer=null,busy=false,pendingConfirmation=null,timerEditorDirty=false,selectedPresentationId=null,pollFailures=0;

const systemDark=window.matchMedia('(prefers-color-scheme: dark)');
let themeChoice='app';
try { themeChoice=localStorage.getItem('flyppt-theme')||'app'; } catch (_) {}
$('themeChoice').value=themeChoice;
function applyTheme(){
  const mode=themeChoice==='app'?(lastState?.uiTheme||'system'):themeChoice;
  const dark=mode==='dark'||(mode==='system'&&systemDark.matches);
  document.documentElement.dataset.theme=dark?'dark':'light';
  document.querySelector('meta[name="theme-color"]').content=dark?'#141a20':'#f4f7f8';
}
$('themeChoice').addEventListener('change',()=>{
  themeChoice=$('themeChoice').value;
  try { localStorage.setItem('flyppt-theme',themeChoice); } catch (_) {}
  applyTheme();
});
systemDark.addEventListener('change',applyTheme);
applyTheme();
function durationStatus(){
  $('durationHint').textContent=timerEditorDirty?'有未保存的时长修改':'已保存';
  $('durationHint').classList.toggle('unsaved',timerEditorDirty);
}
document.addEventListener('keydown',event=>{
  if(event.key==='Enter'&&event.target instanceof HTMLInputElement){event.preventDefault();event.target.blur()}
});
document.addEventListener('pointerdown',event=>{
  const active=document.activeElement;
  if(active instanceof HTMLInputElement&&active!==event.target&&!event.target.closest('input'))active.blur();
});

function url(path){return path+(path.includes('?')?'&':'?')+'token='+encodeURIComponent(token)}
async function api(path,options={}){
  const controller=new AbortController(),timeout=setTimeout(()=>controller.abort(),6000);
  try{
    const response=await fetch(url(path),{cache:'no-store',credentials:'omit',...options,signal:controller.signal});
    const data=await response.json();
    if(!response.ok||data.ok===false)throw new Error(data.message||data.error||'操作失败');
    return data;
  }finally{clearTimeout(timeout)}
}
function notify(text,error=false){const el=$('message');el.textContent=text;el.className='message show'+(error?' error':'');clearTimeout(messageTimer);messageTimer=setTimeout(()=>el.className='message',3200)}
function connection(ok){connected=ok;const el=$('connection');el.textContent=ok?'已连接':'已断开';el.className='status '+(ok?'connected':'disconnected');$('syncText').textContent=ok?'最后同步 '+new Date().toLocaleTimeString(effectiveLanguage==='en'?'en-US':'zh-CN',{hour12:false}):'连接失败；使用 Clash 时请将局域网地址设为 DIRECT'}
function timerState(s){return s.timerState||s}
function setDurationEditor(durationMs){
  const total=Math.max(1,Math.round((Number(durationMs)||0)/1000));
  $('durationHours').value=String(Math.min(23,Math.floor(total/3600)));
  $('durationMinutes').value=String(Math.floor(total%3600/60));
  $('durationSeconds').value=String(total%60);
}
function paint(s){
  // Revisions are monotonic within one application process, not across restarts.
  const instance=s.serverInstance||'';
  if(lastServerInstance!==instance){lastServerInstance=instance;lastPaintedRevision=-1}
  const revision=Number(s.revision);
  if(Number.isFinite(revision)&&revision<lastPaintedRevision)return false;
  if(Number.isFinite(revision))lastPaintedRevision=revision;
  lastState=s;applyTheme();durationStatus();pollFailures=0;connection(true);const t=timerState(s),p=s.presentationState||{};
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
  document.querySelector('.presentation-card').classList.toggle('is-showing',!!p.isSlideShowRunning);
  renderListTools(p);renderPresentations(p.presentations||[]);setAvailability(t,p);updatePresentationHint(p);requestAnimationFrame(syncViewportHeight);if(s.message)notify(s.message);
}
let showHiddenPresentations=false,listGesture=null,listCommit=false,lastMovedId=null,suppressListClickUntil=0;
const listHost=$('presentationList');
const reducedMotion=()=>window.matchMedia('(prefers-reduced-motion: reduce)').matches;
function text(el,value){if(el.textContent!==value)el.textContent=value}
function listRows(){return [...listHost.children].filter(el=>el.classList.contains('presentation-item'))}
function positions(){return new Map(listRows().map(el=>[el.dataset.id,el.getBoundingClientRect().top]))}
function animateOrder(before){
  for(const row of listRows()){
    const old=before.get(row.dataset.id);
    row.getAnimations().forEach(a=>a.cancel());
    const dy=old===undefined?0:old-row.getBoundingClientRect().top;
    if(!reducedMotion()&&Math.abs(dy)>0.5)row.animate([{transform:`translateY(${dy}px)`},{transform:'translateY(0)'}],{duration:280,easing:'cubic-bezier(.22,.75,.25,1)'});
    if(row.dataset.id===lastMovedId){row.classList.add('just-moved');setTimeout(()=>row.classList.remove('just-moved'),650)}
  }
  lastMovedId=null;
}
function fileInfo(item){
  const parts=[];
  if(item.fileSize!=null){const size=Number(item.fileSize);parts.push(size<1024?`${size} B`:size<1048576?`${(size/1024).toFixed(1)} KB`:`${(size/1048576).toFixed(1)} MB`)}
  if(item.modifiedMs!=null)parts.push(new Date(Number(item.modifiedMs)).toLocaleString(effectiveLanguage,{year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit'}));
  return parts.join(' \u00b7 ');
}
function newPresentationRow(item){
  const row=document.createElement('div');row.className='presentation-item';row.dataset.id=item.id;
  const info=document.createElement('div');info.className='file-info';
  for(const cls of ['file-name','file-state','file-directory','file-details']){
    const el=document.createElement(cls==='file-name'?'strong':'span');el.className=cls;info.append(el);
  }
  const handle=document.createElement('button');handle.type='button';handle.className='drag-handle';handle.textContent=String.fromCodePoint(0x283f);
  handle.setAttribute('aria-label',wt("长按拖动排序"));handle.title=wt("长按拖动排序");
  const actions=document.createElement('div');actions.className='presentation-actions';
  function action(label,name,kind){
    const button=document.createElement('button');button.type='button';button.dataset.action=name;
    if(kind==='open')button.dataset.presentationButton='true';else button.dataset.ruleButton='true';
    text(button,wt(label));button.addEventListener('click',()=>{
      if(performance.now()<suppressListClickUntil)return;
      const current=row._item;
      if(kind==='open'){selectedPresentationId=current.id;command('ppt.openPresentation',{presentationId:current.id});return}
      const actual=name==='hide'?(current.mobileHidden?'rules.restore':'rules.hide'):name;
      if(actual.startsWith('rules.move'))lastMovedId=current.id;
      command(actual,{presentationId:current.id,includeHidden:showHiddenPresentations});
    });actions.append(button);
  }
  action("打开",'ppt.openPresentation','open');
  action("隐藏",'hide');
  action("上移",'rules.moveUp');
  action("下移",'rules.moveDown');
  action("移除文件",'rules.delete');
  handle.addEventListener('keydown',event=>{
    if(event.key==='ArrowUp'||event.key==='ArrowDown'){
      event.preventDefault();lastMovedId=row.dataset.id;
      command(event.key==='ArrowUp'?'rules.moveUp':'rules.moveDown',{presentationId:row.dataset.id,includeHidden:showHiddenPresentations});
    }
  });
  row.append(info,handle,actions);return row;
}
function renderPresentations(items){
  if(listGesture?.active||listCommit)return;
  const controlled=items.filter(x=>x.isRule!==false),active=controlled.find(x=>x.isActive);
  if(active)selectedPresentationId=active.id;
  else if(!controlled.some(x=>x.id===selectedPresentationId))selectedPresentationId=null;
  const previousOrder=listRows().map(row=>row.dataset.id).join('\n');
  const before=positions(),visible=controlled.filter(x=>showHiddenPresentations||!x.mobileHidden),ids=new Set(visible.map(x=>x.id));
  for(const row of listRows())if(!ids.has(row.dataset.id))row.remove();
  const existing=new Map(listRows().map(el=>[el.dataset.id,el]));
  visible.forEach((item,index)=>{
    const row=existing.get(item.id)||newPresentationRow(item);row._item=item;
    row.dataset.active=String(!!item.isActive);row.dataset.showing=String(!!item.isSlideShowRunning);
    if(item.isSlideShowRunning)row.setAttribute('aria-current','true');else row.removeAttribute('aria-current');
    text(row.querySelector('.file-name'),item.name);row.querySelector('.file-name').title=item.name;
    text(row.querySelector('.file-state'),wt(item.isSlideShowRunning?"正在放映":item.isActive?"当前活动":item.isOpen?"已打开":"受控文件"));
    text(row.querySelector('.file-directory'),item.directory||'');row.querySelector('.file-directory').title=item.directory||'';
    text(row.querySelector('.file-details'),fileInfo(item));
    text(row.querySelector('[data-presentation-button]'),effectiveLanguage==='en'&&item.isActive?'Active':wt(item.isActive?"当前":item.isOpen?"切换":"打开"));
    text(row.querySelector('[data-action="hide"]'),wt(item.mobileHidden?"恢复":"隐藏"));
    row.querySelector('[data-action="rules.moveUp"]').dataset.edge=String(index===0);
    row.querySelector('[data-action="rules.moveDown"]').dataset.edge=String(index===visible.length-1);
    if(listHost.children[index]!==row)listHost.insertBefore(row,listHost.children[index]||null);
  });
  $('listEmpty').hidden=visible.length>0;text($('listEmpty'),wt(controlled.length?"所有文件已隐藏":"暂无受控文件"));
  // Timer/status polling must not cancel a movement already in progress.
  if(previousOrder!==visible.map(item=>item.id).join('\n'))animateOrder(before);
  refreshPresentationButtons();
}
function renderListTools(p){
  if(!listGesture?.active&&!listCommit){
    $('listSort').value=p.listSort||'manual';
    text($('listSortDirection'),wt(p.listSortDescending?"降序":"升序"));
    text($('listShowHidden'),wt(showHiddenPresentations?"收起隐藏项":"显示隐藏项"));
    $('listShowHidden').setAttribute('aria-pressed',String(showHiddenPresentations));
  }
  const select=$('availablePresentations'),available=p.availablePresentations||[],signature=JSON.stringify(available.map(x=>[x.id,x.name]));
  if(select.dataset.signature!==signature){
    const selected=select.value;select.replaceChildren();select.dataset.signature=signature;
    if(!available.length){const option=document.createElement('option');option.value='';text(option,wt("没有可添加的已打开文件"));select.append(option)}
    for(const item of available){const option=document.createElement('option');option.value=item.id;option.textContent=item.name;select.append(option)}
    if(available.some(x=>x.id===selected))select.value=selected;
  }
}
function refreshPresentationButtons(){
  const blocked=busy||!connected||!!lastState?.presentationState?.isOperationBusy||!!listGesture?.active||listCommit;
  document.querySelectorAll('[data-rule-button]').forEach(button=>button.disabled=blocked||button.dataset.edge==='true');
  document.querySelectorAll('[data-presentation-button]').forEach(button=>button.disabled=blocked||button.closest('.presentation-item')?.dataset.active==='true');
  document.querySelectorAll('.drag-handle').forEach(button=>button.disabled=blocked&&!listGesture?.active);
  $('listSort').disabled=blocked;$('listSortDirection').disabled=blocked||$('listSort').value==='manual';$('listShowHidden').disabled=blocked;
  $('availablePresentations').disabled=blocked||!$('availablePresentations').value;$('addOpenFile').disabled=blocked||!$('availablePresentations').value;
}
$('listSort').addEventListener('change',async()=>{
  const ok=await command('rules.sort',{sortBy:$('listSort').value,descending:false});
  if(!ok)renderListTools(lastState?.presentationState||{});
});
$('listSortDirection').addEventListener('click',()=>command('rules.sort',{sortBy:$('listSort').value,descending:!lastState?.presentationState?.listSortDescending}));
$('listShowHidden').addEventListener('click',()=>{showHiddenPresentations=!showHiddenPresentations;renderListTools(lastState?.presentationState||{});renderPresentations(lastState?.presentationState?.presentations||[]);requestAnimationFrame(syncViewportHeight)});
$('addOpenFile').addEventListener('click',()=>{const id=$('availablePresentations').value;if(id)command('rules.addOpen',{presentationId:id})});
$('addOpenDetails').addEventListener('toggle',()=>requestAnimationFrame(syncViewportHeight));

// A stationary hold enters drag mode; ordinary swipes still scroll the list.
// Only one POST is sent on drop; cancellation, polling and reconnection send none.
function startListGesture(target,x,y){
  if(listGesture||busy||!connected||lastState?.presentationState?.isOperationBusy||listCommit)return;
  const row=target.closest('.presentation-item');
  if(!row||target.closest('.presentation-actions')||target.closest('input,select'))return;
  listGesture={row,id:row.dataset.id,x,y,startX:x,startY:y,active:false,timer:null,raf:null,ghost:null,
    expected:(lastState?.presentationState?.presentations||[]).map(x=>x.id),original:listRows().map(x=>x.dataset.id)};
  listGesture.timer=setTimeout(()=>{
    const g=listGesture;if(!g||!g.row.isConnected)return;
    g.active=true;const r=g.row.getBoundingClientRect();g.offset=y-r.top;g.left=r.left;
    g.ghost=g.row.cloneNode(true);g.ghost.classList.add('drag-ghost');g.ghost.inert=true;g.ghost.setAttribute('aria-hidden','true');
    Object.assign(g.ghost.style,{width:`${r.width}px`,left:`${r.left}px`,top:`${r.top}px`});document.body.append(g.ghost);
    g.row.classList.add('drag-placeholder');swipeStart=null;refreshPresentationButtons();
    function tick(){
      if(listGesture!==g||!g.active)return;
      const bounds=listHost.getBoundingClientRect(),edge=36;
      const delta=g.y<bounds.top+edge?-10:g.y>bounds.bottom-edge?10:0;
      if(delta){const old=listHost.scrollTop;listHost.scrollTop+=delta;if(old!==listHost.scrollTop)placeDraggedRow(g)}
      g.raf=requestAnimationFrame(tick);
    }
    g.raf=requestAnimationFrame(tick);
  },360);
}
function placeDraggedRow(g){
  const bounds=listHost.getBoundingClientRect();
  const before=listRows().filter(row=>row!==g.row).find(row=>g.y<bounds.top+row.offsetTop-listHost.scrollTop+row.offsetHeight/2)||null;
  if(before===g.row.nextElementSibling||(!before&&!g.row.nextElementSibling))return;
  const old=positions();listHost.insertBefore(g.row,before);animateOrder(old);
}
function moveListGesture(x,y,event){
  const g=listGesture;if(!g)return;
  if(!g.active){if(Math.hypot(x-g.startX,y-g.startY)>8)finishListGesture(true);return}
  if(event.cancelable)event.preventDefault();
  g.x=x;g.y=y;g.ghost.style.top=`${y-g.offset}px`;placeDraggedRow(g);
}
async function finishListGesture(cancelled=false){
  const g=listGesture;if(!g)return;clearTimeout(g.timer);cancelAnimationFrame(g.raf);
  if(!g.active){listGesture=null;return}
  const before=g.row.nextElementSibling?.dataset.id||null,current=listRows().map(x=>x.dataset.id),changed=JSON.stringify(current)!==JSON.stringify(g.original);
  g.ghost?.remove();g.row.classList.remove('drag-placeholder');listGesture=null;suppressListClickUntil=performance.now()+650;
  if(!cancelled&&changed&&connected&&!busy){
    listCommit=true;
    const ok=await command('rules.move',{presentationId:g.id,beforePresentationId:before,expectedOrder:g.expected,includeHidden:showHiddenPresentations});
    listCommit=false;
    if(ok){lastMovedId=g.id;notify(wt("顺序已保存"))}
  }
  renderListTools(lastState?.presentationState||{});renderPresentations(lastState?.presentationState?.presentations||[]);refreshPresentationButtons();
}
listHost.addEventListener('touchstart',event=>{if(event.touches.length!==1){finishListGesture(true);return}const t=event.touches[0];startListGesture(event.target,t.clientX,t.clientY)},{passive:true});
listHost.addEventListener('touchmove',event=>{if(event.touches.length!==1){finishListGesture(true);return}const t=event.touches[0];moveListGesture(t.clientX,t.clientY,event)},{passive:false});
listHost.addEventListener('touchend',()=>finishListGesture(false),{passive:true});
listHost.addEventListener('touchcancel',()=>finishListGesture(true),{passive:true});
listHost.addEventListener('pointerdown',event=>{if(event.pointerType==='touch'||event.button!==0)return;startListGesture(event.target,event.clientX,event.clientY)});
document.addEventListener('pointermove',event=>{if(event.pointerType!=='touch')moveListGesture(event.clientX,event.clientY,event)});
document.addEventListener('pointerup',event=>{if(event.pointerType!=='touch')finishListGesture(false)});
document.addEventListener('pointercancel',event=>{if(event.pointerType!=='touch')finishListGesture(true)});
document.addEventListener('click',event=>{if(performance.now()<suppressListClickUntil&&event.target.closest('.presentation-list')){event.preventDefault();event.stopPropagation()}},true);
document.addEventListener('keydown',event=>{if(event.key==='Escape'&&listGesture)finishListGesture(true)});
window.addEventListener('blur',()=>finishListGesture(true));
document.addEventListener('visibilitychange',()=>{if(document.hidden)finishListGesture(true)});
for(const name of ['copy','cut','selectstart','contextmenu'])document.addEventListener(name,event=>event.preventDefault());

function setAvailability(t,p){
  const controlled=!!p.currentControllable,show=!!p.isSlideShowRunning&&controlled,has=controlled||!!selectedPresentationId,state=t.state||'',running=!!t.running,paused=state.includes('暂停'),stopped=!running&&!paused,operationBusy=!!p.isOperationBusy;
  commandButtons.forEach(button=>{
    const cmd=button.dataset.command;let disabled=busy||operationBusy||!connected;
    if(cmd==='timer.start')disabled||=!stopped;
    else if(cmd==='timeup.dismiss')disabled||=!t.timeUpBlackoutActive;
    else if(cmd==='timer.pause')disabled||=!running;
    else if(cmd==='timer.resume')disabled||=!paused;
    else if(cmd.startsWith('ppt.')){
      if(['ppt.previous','ppt.next','ppt.gotoSlide','ppt.endShow','ppt.blackScreenToggle','ppt.whiteScreenToggle'].includes(cmd))disabled||=!show;
      if(['ppt.startFromBeginning','ppt.startFromCurrent'].includes(cmd))disabled||=!has;
      if(cmd==='ppt.closeActivePresentation')disabled||=!controlled;
      if(cmd==='ppt.closeCurrentPresentation')disabled||=!p.canCloseLast;
      if(cmd==='ppt.forceQuitAll')disabled||=!p.canQuitAll;
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
  $('presentationHint').textContent=p.error||(p.hasPresentation&&!p.currentControllable?"当前文稿未受控，请先加入列表。":!p.powerPointInstalled?'本机未安装 Microsoft PowerPoint':!p.hasPresentation?'请先打开或从上方列表选择演示文稿':stale?'PowerPoint 状态可能已过期，计时控制仍可使用':'');
}
function schedulePoll(delay=1000){clearTimeout(pollTimer);pollTimer=setTimeout(poll,delay)}
async function poll(){
  const epoch=stateEpoch,sequence=++pollSequence;
  try{const result=await api('/state');if(epoch===stateEpoch&&!busy&&sequence>lastPaintedPoll){lastPaintedPoll=sequence;paint(result)}schedulePoll(1000)}
  catch(e){
    if(epoch!==stateEpoch||busy){schedulePoll(1000);return}
    pollFailures+=1;
    if(pollFailures>=3){
      connection(false);setAvailability({},{});refreshPresentationButtons();
      if(pollFailures===3)notify('连接失败：'+e.message+'。正在自动重连；Clash/TUN 请将本机局域网 IP 和端口设为 DIRECT。',true);
    }
    schedulePoll(Math.min(5000,500+pollFailures*750));
  }
}
function requestConfirmation(name,extra){
  const details={
    'rules.delete':["移除文件", "移除后不再显示在受控列表，也不能远程打开或关闭。不删除磁盘文件，不关闭当前文稿；需要时可重新加入。"],
    'ppt.endShow':['结束放映','仅结束电脑端当前放映，不关闭文稿或 PowerPoint。'],
    'ppt.closeActivePresentation':['关闭当前文稿','关闭当前活动文稿且不保存；其他已打开文稿保持不变。'],
    'ppt.closeCurrentPresentation':['关闭最后打开的文稿','将按打开顺序关闭最后打开的文稿且不保存；每次只关闭一个。'],
    'ppt.forceQuitAll':['退出演示软件','将强制退出电脑端全部 PowerPoint/WPS/演示软件，未保存内容可能丢失。']
  }[name];
  if(!details)return false;
  pendingConfirmation={name,extra};
  $('confirmTitle').textContent=details[0];$('confirmText').textContent=details[1];showConfirmation();
  return true;
}
let confirmationFocus=null;
function showConfirmation(){
  confirmationFocus=document.activeElement;
  const panel=$('confirmPanel');
  panel.hidden=false;
  for(const child of panel.parentElement.children){if(child!==panel)child.inert=true}
  $('confirmAccept').focus();
}
function closeConfirmation(){
  for(const child of $('confirmPanel').parentElement.children)child.inert=false;
  pendingConfirmation=null;$('confirmPanel').hidden=true;if(confirmationFocus?.isConnected)confirmationFocus.focus();confirmationFocus=null;$('confirmCancel').textContent='取消';$('confirmAccept').textContent='确认';$('confirmAccept').classList.add('danger')}
function requestDurationConfirmation(durationMs,ruleCount){
  pendingConfirmation={name:'timer.setDuration',extra:{durationMs},durationChoice:true};
  $('confirmTitle').textContent='同步文件规则时长？';$('confirmText').textContent=`当前已有 ${ruleCount} 个待控演示文稿。是否把新时长同步应用到全部文件规则？`;
  $('confirmCancel').textContent='仅修改全局';$('confirmAccept').textContent='同步全部';$('confirmAccept').classList.remove('danger');showConfirmation();
}
async function command(name,extra={}){
  if(!connected||busy){if(!connected)notify('当前已断开，命令未发送',true);return false}
  if(!extra.confirmed&&requestConfirmation(name,extra))return false;
  stateEpoch+=1;busy=true;setAvailability(timerState(lastState||{}),(lastState||{}).presentationState||{});
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
pagesViewport.addEventListener('touchstart',event=>{if(event.touches.length!==1||event.target.closest('.confirm-panel,.presentation-list'))return;const touch=event.touches[0];swipeStart={x:touch.clientX,y:touch.clientY,time:performance.now(),baseX:freezeTrack(),dragging:false,lastX:touch.clientX,lastTime:performance.now()}},{passive:true});
pagesViewport.addEventListener('touchmove',event=>{if(!swipeStart||!event.touches.length)return;const touch=event.touches[0],dx=touch.clientX-swipeStart.x,dy=touch.clientY-swipeStart.y;if(!swipeStart.dragging){const distance=Math.hypot(dx,dy);if(distance<SWIPE_DIRECTION_DISTANCE)return;const angle=Math.atan2(Math.abs(dy),Math.abs(dx))*180/Math.PI;if(angle>SWIPE_MAX_ANGLE_DEGREES){swipeStart=null;renderPage(pageIndex,true);return}swipeStart.dragging=true;suppressSwipeClick=true;clearTimeout(suppressSwipeClickTimer)}event.preventDefault();const width=Math.max(1,pagesViewport.clientWidth),minX=-(pages.length-1)*width;let nextX=swipeStart.baseX+dx;if(nextX>0)nextX*=.24;else if(nextX<minX)nextX=minX+(nextX-minX)*.24;pagesTrack.style.transition='none';pagesTrack.style.transform=`translate3d(${nextX}px,0,0)`;swipeStart.lastX=touch.clientX;swipeStart.lastTime=performance.now()},{passive:false});
pagesViewport.addEventListener('touchend',event=>{if(!swipeStart)return;const touch=event.changedTouches[0],now=performance.now(),x=touch?touch.clientX:swipeStart.lastX,dx=x-swipeStart.x,elapsed=Math.max(1,now-swipeStart.time),dragging=swipeStart.dragging,currentX=readTrackX(),width=Math.max(1,pagesViewport.clientWidth);swipeStart=null;if(!dragging){renderPage(pageIndex,true);return}const velocity=dx/elapsed,commit=Math.abs(dx)>=18||Math.abs(velocity)>=.12,position=-currentX/width;let target=Math.round(position);if(commit)target=dx<0?Math.floor(position)+1:Math.ceil(position)-1;renderPage(target,true);suppressSwipeClickTimer=setTimeout(()=>suppressSwipeClick=false,420)},{passive:true});
pagesViewport.addEventListener('touchcancel',()=>{swipeStart=null;renderPage(pageIndex,true)},{passive:true});
document.addEventListener('click',event=>{if(!suppressSwipeClick)return;suppressSwipeClick=false;clearTimeout(suppressSwipeClickTimer);event.preventDefault();event.stopPropagation() },true);
pagesTrack.addEventListener('transitionend',event=>{if(event.propertyName==='transform')syncViewportHeight()});
window.addEventListener('resize',()=>{renderPage(pageIndex,false);requestAnimationFrame(syncViewportHeight)});
renderPage(0,false);
$('confirmCancel').addEventListener('click',()=>{const pending=pendingConfirmation;if(pending?.durationChoice){closeConfirmation();command(pending.name,{...pending.extra,syncAllRules:false,confirmed:true}).then(ok=>{if(ok){timerEditorDirty=false;durationStatus();notify('已修改全局时长，文件规则保持不变')}})}else closeConfirmation()});
$('confirmAccept').addEventListener('click',()=>{const pending=pendingConfirmation;closeConfirmation();if(pending)command(pending.name,{...pending.extra,...(pending.durationChoice?{syncAllRules:true}:{}),confirmed:true}).then(ok=>{if(ok&&pending.durationChoice){timerEditorDirty=false;durationStatus();notify('已同步修改全部文件规则时长')}})});
commandButtons.forEach(button=>button.addEventListener('click',()=>{
  const name=button.dataset.command;
  command(name,['timer.restart','ppt.startFromBeginning','ppt.startFromCurrent'].includes(name)?{presentationId:selectedPresentationId}:{});
}));
$('durationHours').addEventListener('input',()=>{const ms=(Number($('durationHours').value)*3600+Number($('durationMinutes').value)*60+Number($('durationSeconds').value))*1000;timerEditorDirty=ms!==Number(timerState(lastState||{}).durationMs);durationStatus()});
$('durationMinutes').addEventListener('input',()=>{const ms=(Number($('durationHours').value)*3600+Number($('durationMinutes').value)*60+Number($('durationSeconds').value))*1000;timerEditorDirty=ms!==Number(timerState(lastState||{}).durationMs);durationStatus()});
$('durationSeconds').addEventListener('input',()=>{const ms=(Number($('durationHours').value)*3600+Number($('durationMinutes').value)*60+Number($('durationSeconds').value))*1000;timerEditorDirty=ms!==Number(timerState(lastState||{}).durationMs);durationStatus()});
$('applyDuration').addEventListener('click',async()=>{
  const hours=Number($('durationHours').value),minutes=Number($('durationMinutes').value),seconds=Number($('durationSeconds').value);
  if(!Number.isInteger(hours)||hours<0||hours>23||!Number.isInteger(minutes)||minutes<0||minutes>59||!Number.isInteger(seconds)||seconds<0||seconds>59){notify('请输入有效的时、分、秒',true);return}
  const durationMs=(hours*3600+minutes*60+seconds)*1000;
  if(durationMs<=0){notify('计时时长必须大于 0 秒',true);return}
  const ruleCount=Number(timerState(lastState||{}).ruleCount)||0;
  if(ruleCount>0){requestDurationConfirmation(durationMs,ruleCount);return}
  if(await command('timer.setDuration',{durationMs,syncAllRules:false})){timerEditorDirty=false;durationStatus();notify('计时时长已同步到电脑')}
});
timerModeButtons.forEach(button=>button.addEventListener('click',async()=>{if(await command('timer.setMode',{mode:button.dataset.timerMode,presentationId:selectedPresentationId}))notify(`已切换为${button.dataset.timerMode==='countup'?'正计时':'倒计时'}`)}));
$('gotoSlide').addEventListener('click',()=>{const input=$('slideNumber'),value=Number(input.value),max=Number(input.max);if(!Number.isInteger(value)||value<1||value>max){notify(`请输入 1 到 ${max} 之间的页码`,true);input.focus();return}command('ppt.gotoSlide',{slideNumber:value})});
window.addEventListener('online',()=>{if(!busy)schedulePoll(0)});
window.addEventListener('focus',()=>{if(!busy)schedulePoll(0)});
document.addEventListener('visibilitychange',()=>{if(!document.hidden&&!busy)schedulePoll(0)});
poll();

$('confirmPanel').addEventListener('keydown',event=>{
  if(event.key==='Escape'){event.preventDefault();closeConfirmation();return}
  if(event.key==='Tab'){
    event.preventDefault();
    (document.activeElement===$('confirmAccept')?$('confirmCancel'):$('confirmAccept')).focus();
  }
});
