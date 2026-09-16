"""Capture the unchanged shipping Web Remote UI with documented example state.
Requires Playwright + Chromium. This is a documentation fixture, not a phone/Office test.
No live token, private path or presentation contents are used.
"""
import argparse, json
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "src/FlyPPTTimer/Web"
OUT = ROOT / "docs/media/v1.15.0"

def sample_state(language, theme):
    english = language == "en"
    names = (["01_Welcome.pptx", "02_Keynote.pptx", "03_Discussion.pptx"] if english else
             ["01_开场介绍.pptx", "02_主题汇报.pptx", "03_讨论交流.pptx"])
    items = [dict(id=f"demo-{i}", name=n, directory="C:\\Presentations",
                  isRule=True, mobileHidden=False, isOpen=i<2, isActive=i==1,
                  isSlideShowRunning=i==1, isManaged=True,
                  fileSize=1048576*(i+1), modifiedMs=1789203600000) for i,n in enumerate(names)]
    timer = dict(mode="倒计时", state="运行中", running=True, durationMs=480000,
                 elapsedMs=168000, remainingMs=312000, displayText="05:12", isOvertime=False,
                 continueOvertime=True, unlimited=False, windowVisible=True, muted=False, timeUpBlackoutActive=False,
                 ruleCount=len(items))
    ppt = dict(powerPointInstalled=True, powerPointRunning=True, hasPresentation=True,
               isSlideShowRunning=True, presentationName=names[1],
               presentationPath="C:\\Presentations\\"+names[1], currentSlide=7, totalSlides=24,
               screenMode="正常", updatedAt="2026-09-13T08:00:00+08:00", error="", presentations=items,
               operation="", operationMessage="", operationStartedAt=None, operationId="",
               isOperationBusy=False, isCurrentPresentationManaged=True, openPresentationCount=2,
               wpsDetected=False, availablePresentations=[], currentControllable=True,
               canCloseLast=True, canQuitAll=True, listSort="manual", listSortDescending=False)
    return dict(ok=True,message="",timerState=timer,presentationState=ppt,**timer,
                fileBrowsingEnabled=True, slideTiming=dict(presentationName=names[1],currentSlide=7,currentSeconds=23,totalSlides=24,
                    pages=[dict(slide=i,seconds=seconds) for i,seconds in enumerate([31,42,18,65,27,36,23],1)]),
                scenarios=[
                    dict(id="scenario-1",name="Competition" if english else "竞赛模式",badgeColor="#E53935",active=True),
                    dict(id="scenario-2",name="Recruiting" if english else "人事招聘",badgeColor="#1E88E5",active=False),
                    dict(id="scenario-3",name="Speaker Reminder" if english else "演讲者提醒",badgeColor="#43A047",active=False)],
                activeScenarioId="scenario-1",
                connectedClients=1,version="1.15.0",revision=1,serverInstance="docs-fixture",uiTheme=theme)

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--browser", help="Optional Chromium executable")
    args=parser.parse_args()
    OUT.mkdir(parents=True,exist_ok=True)
    with sync_playwright() as pw:
        browser=pw.chromium.launch(headless=True,executable_path=args.browser)
        for language in ["en","zh-CN"]:
            for theme in ["light","dark"]:
                state=sample_state(language,theme)
                context=browser.new_context(viewport=dict(width=390,height=844),device_scale_factor=2,
                    is_mobile=True,has_touch=True,locale=language,color_scheme=theme,timezone_id="Asia/Shanghai")
                page=context.new_page()
                page.set_default_timeout(5000)
                html=(WEB/"index.html").read_text(encoding="utf-8").replace("__FLYPPT_TOKEN__","documentation-example")
                import re
                html=re.sub(r'<link[^>]+rel="stylesheet"[^>]*>', '', html)
                html=re.sub(r'<script[^>]+src=[^>]*></script>', '', html)
                page.set_content(html)
                page.add_style_tag(path=WEB/"app.css")
                # In-memory response fixture: no network and no server access required.
                page.evaluate("""state => { window.fetch = async (path, options={}) => {
                    if(path.startsWith('/browse')){
                        const request=JSON.parse(options.body||'{}');
                        const listing={ok:true,path:request.path||'C:\\\\Presentations',parent:'C:\\\\',entries:[
                            {name:'Archive',path:'C:\\\\Presentations\\\\Archive',isDirectory:true},
                            {name:'01_Welcome.pptx',path:'C:\\\\Presentations\\\\01_Welcome.pptx',isDirectory:false},
                            {name:'02_Keynote.pptx',path:'C:\\\\Presentations\\\\02_Keynote.pptx',isDirectory:false},
                            {name:'03_Workshop.pptx',path:'C:\\\\Presentations\\\\03_Workshop.pptx',isDirectory:false}]};
                        return new Response(JSON.stringify(listing),{status:200});
                    }
                    state.presentationState.updatedAt = new Date().toISOString();
                    return new Response(JSON.stringify(state), {status: 200,
                    headers: {'Content-Type':'application/json'}});
                }; }""", state)
                page.add_script_tag(path=WEB/"app.js")
                page.locator("#connection.connected").wait_for()
                page.wait_for_timeout(350)
                page.screenshot(animations="disabled", path=OUT/f"mobile-{language}-{theme}-timer.png",full_page=True)
                page.locator("#scenarioCard .select-trigger").click()
                page.locator(".select-popup.visible").wait_for()
                page.wait_for_timeout(180)
                page.screenshot(animations="disabled", path=OUT/f"mobile-{language}-{theme}-scenario.png",full_page=True)
                page.keyboard.press("Escape")
                page.locator('[data-page="pptPage"]').click()
                page.wait_for_timeout(350)
                page.screenshot(animations="disabled", path=OUT/f"mobile-{language}-{theme}-presentation.png",full_page=True)
                page.locator("#slideHistory").evaluate("(e)=>e.open=true")
                page.wait_for_timeout(100)
                page.screenshot(animations="disabled", path=OUT/f"mobile-{language}-{theme}-slide-times.png",full_page=True)
                page.locator("#browseComputer").click()
                page.locator(".browser-entry").first.wait_for()
                page.screenshot(animations="disabled", path=OUT/f"mobile-{language}-{theme}-browser.png")
                page.locator("#browseClose").click()
                page.evaluate("window.scrollTo(0,0)")
                page.wait_for_timeout(150)
                page.locator(".theme-row .select-trigger").click()
                page.locator(".select-popup.visible").wait_for()
                page.wait_for_timeout(200)
                page.screenshot(animations="disabled", path=OUT/f"mobile-{language}-{theme}-select.png")
                context.close()
        browser.close()
    print("Captured twenty screenshots from the unchanged Web Remote HTML/CSS/JS.")

if __name__=="__main__":
    main()
