"""Capture the unchanged shipping Web Remote UI with documented example state.
Requires Playwright + Chromium. This is a documentation fixture, not a phone/Office test.
No live token, private path or presentation contents are used.
"""
import argparse, json
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "src/FlyPPTTimer/Web"
OUT = ROOT / "docs/media/v1.13.1"

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
                 continueOvertime=True, windowVisible=True, muted=False, timeUpBlackoutActive=False,
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
                connectedClients=1,version="1.13.1",revision=1,serverInstance="docs-fixture",uiTheme=theme)

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
                html=(WEB/"index.html").read_text(encoding="utf-8").replace("__FLYPPT_TOKEN__","documentation-example")
                import re
                html=re.sub(r'<link[^>]+rel="stylesheet"[^>]*>', '', html)
                html=re.sub(r'<script[^>]+src=[^>]*></script>', '', html)
                page.set_content(html)
                page.add_style_tag(path=WEB/"app.css")
                # In-memory response fixture: no network and no server access required.
                page.evaluate("""state => { window.fetch = async () => {
                    state.presentationState.updatedAt = new Date().toISOString();
                    return new Response(JSON.stringify(state), {status: 200,
                    headers: {'Content-Type':'application/json'}});
                }; }""", state)
                page.add_script_tag(path=WEB/"app.js")
                page.locator("#connection.connected").wait_for()
                page.wait_for_timeout(350)
                page.screenshot(path=OUT/f"mobile-{language}-{theme}-timer.png",full_page=True)
                page.locator('[data-page="pptPage"]').click()
                page.wait_for_timeout(350)
                page.screenshot(path=OUT/f"mobile-{language}-{theme}-presentation.png",full_page=True)
                context.close()
        browser.close()
    print("Captured eight screenshots from the unchanged Web Remote HTML/CSS/JS.")

if __name__=="__main__":
    main()
