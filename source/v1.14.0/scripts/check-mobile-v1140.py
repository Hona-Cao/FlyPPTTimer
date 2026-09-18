from pathlib import Path
import argparse
import importlib.util,json,re
from playwright.sync_api import sync_playwright
root=Path(__file__).resolve().parents[1];web=root/'src/FlyPPTTimer/Web'
parser=argparse.ArgumentParser();parser.add_argument('--browser');args=parser.parse_args()
spec=importlib.util.spec_from_file_location('fixture',root/'scripts/capture-mobile.py');fixture=importlib.util.module_from_spec(spec);spec.loader.exec_module(fixture)
out=root/'artifacts/mobile-checks';out.mkdir(exist_ok=True,parents=True)
checks=[]
with sync_playwright() as pw:
    browser=pw.chromium.launch(headless=True,executable_path=args.browser)
    for language in ['en','zh-CN']:
        context=browser.new_context(viewport={'width':390,'height':844},is_mobile=True,has_touch=True,locale=language)
        p=context.new_page(); errors=[];p.on('pageerror',lambda e:errors.append(str(e)))
        state=fixture.sample_state(language,'light');state.update(version='1.14.0',fileBrowsingEnabled=True,slideTiming=dict(presentationName='Keynote.pptx',currentSlide=7,currentSeconds=23,totalSlides=24,pages=[dict(slide=1,seconds=38),dict(slide=7,seconds=23)]))
        html=web.joinpath('index.html').read_text();html=re.sub(r'<link[^>]+rel="stylesheet"[^>]*>','',html);html=re.sub(r'<script[^>]+src=[^>]*></script>','',html)
        p.set_content(html);p.add_style_tag(path=web/'app.css')
        p.evaluate(r'''state=>{window.testState=state;window.calls=[];window.fetch=async(path,options={})=>{
          window.calls.push({path,body:options.body});const data=JSON.parse(options.body||'{}');
          if(path.startsWith('/browse'))return new Response(JSON.stringify({ok:true,path:data.path||'',parent:'',entries:data.path?[{name:'Report.pptx',path:'C:\\Talks\\Report.pptx',isDirectory:false}]:[{name:'Talks',path:'C:\\Talks',isDirectory:true}]}),{status:200});
          if(data.command==='rules.addFile'){state.presentationState.presentations.push({id:data.presentationId,name:'Report.pptx',isRule:true,isManaged:true});state.revision++;}
          if(data.command==='rules.sort'){state.presentationState.listSort=data.sort;state.revision++;}
          state.presentationState.updatedAt=new Date().toISOString();return new Response(JSON.stringify(state),{status:200});};}''',state)
        p.add_script_tag(path=web/'app.js');p.locator('#connection.connected').wait_for()
        assert not errors,errors
        assert p.locator('.select-widget').count()==3
        trigger=p.locator('.theme-row .select-trigger');trigger.click();p.locator('.select-popup.visible').wait_for()
        p.locator('.select-option').last.click();assert p.locator('#themeChoice').input_value()=='dark'
        assert p.locator('html').get_attribute('data-theme')=='dark'
        trigger.press('ArrowDown');p.locator('.select-popup').wait_for();p.keyboard.press('Home');p.keyboard.press('Enter')
        assert p.locator('#themeChoice').input_value()=='app'
        trigger.click();p.keyboard.press('Escape');assert p.locator('.select-popup').count()==0
        checks.append(language+': dropdowns themed, value sync, keyboard and Escape')
        p.locator('[data-page="pptPage"]').click();p.wait_for_timeout(400)
        assert p.locator('#slideElapsed').inner_text()=='23'
        assert p.locator('#slideHistoryRows tr').count()==2
        assert p.locator('.nav-grid').bounding_box()['y']<p.locator('#controlledFilesCard').bounding_box()['y']
        checks.append(language+': controls before list; current and accumulated slide seconds')
        p.locator('#browseComputer').click();p.locator('.folder-button').wait_for();p.locator('.folder-button').click()
        p.locator('.browser-entry button').wait_for();assert p.locator('.browser-entry').count()==1
        p.locator('.browser-entry button').click();p.wait_for_timeout(200)
        assert p.locator('.browser-entry button').is_disabled()
        assert p.evaluate('testState.presentationState.presentations.some(x=>x.name==="Report.pptx")')
        p.locator('#browseFilter').fill('not-found');assert p.locator('.browser-entry').count()==0
        p.locator('#browseFilter').fill('');p.screenshot(path=out/f'{language}-browser.png')
        p.keyboard.press('Escape');assert p.locator('#fileBrowser').is_hidden();assert not p.locator('#pagesViewport').evaluate('(el)=>el.inert')
        checks.append(language+': folder browse, add, deduplicate, filter, Escape/focus restore')
        p.evaluate('testState.fileBrowsingEnabled=false;testState.revision++;paint(testState)')
        old=p.evaluate('calls.filter(x=>x.path.startsWith("/browse")).length')
        p.locator('#browseComputer').click();p.wait_for_timeout(200)
        assert p.evaluate('calls.filter(x=>x.path.startsWith("/browse")).length')==old
        assert p.locator('.browser-entry').count()==0
        p.locator('#browseClose').click()
        checks.append(language+': PC permission required; disabled view never requests folders')
        assert not errors,errors
        p.locator('#slideHistory').evaluate('(e)=>e.open=true');p.wait_for_timeout(200);p.screenshot(path=out/f'{language}-presentation.png',full_page=True)
        context.close()
    browser.close()
(out/'results.json').write_text(json.dumps(dict(passed=checks,failed=[]),indent=2))
print(json.dumps(checks,indent=2))
