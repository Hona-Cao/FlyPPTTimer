$ErrorActionPreference = 'Stop'
Add-Type @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public static class RC33Native {
  public delegate bool EnumProc(IntPtr h, IntPtr p);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc f, IntPtr p);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder b, int n);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint m, UIntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out Rect r);
  [StructLayout(LayoutKind.Sequential)] public struct Rect { public int left, top, right, bottom; }
  public static IntPtr Find(int pid, string title, bool visible) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h,p)=> { uint wp; GetWindowThreadProcessId(h,out wp); if(wp!=(uint)pid) return true;
      var b=new StringBuilder(512); GetWindowText(h,b,b.Capacity);
      if(b.ToString()==title && (!visible || IsWindowVisible(h))){found=h;return false;} return true; }, IntPtr.Zero);
    return found;
  }
  public static string Size(IntPtr h) { Rect r; if(!GetClientRect(h,out r))throw new Exception("GetClientRect failed");
    if(r.right<300 || r.bottom<200) throw new Exception("Invalid management client geometry");
    return r.right+"x"+r.bottom; }
}
'@
$stage=Join-Path $env:RUNNER_TEMP 'rc33-native-smoke'
New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item target/release/FlyPPTTimer.exe $stage
foreach($dll in @('vcruntime140.dll','vcruntime140_1.dll','msvcp140.dll')) {
  Copy-Item (Join-Path $env:WINDIR "System32/$dll") $stage
}
'{"Language":"en","RemoteControl":{"Enabled":false},"Update":{"CheckOnStartup":false}}' | Set-Content (Join-Path $stage 'FlyPPTTimer.config.json') -Encoding utf8
$app=Start-Process (Join-Path $stage 'FlyPPTTimer.exe') -WorkingDirectory $stage -PassThru
function Wait-Window([string]$title,[bool]$visible=$true) {
  for($i=0;$i -lt 60;$i++) { $h=[RC33Native]::Find($app.Id,$title,$visible); if($h -ne [IntPtr]::Zero){return $h}; Start-Sleep -Milliseconds 100 }
  throw "Window not found: $title; process exited=$($app.HasExited)"
}
$results=[Collections.Generic.List[string]]::new()
try {
  $tray=Wait-Window 'FlyPPTTimerDesktopWindow' $false
  for($cycle=0;$cycle -lt 12;$cycle++) {
    [RC33Native]::PostMessage($tray,0x111,[UIntPtr]1004,[IntPtr]::Zero) | Out-Null
    $settings=Wait-Window 'FlyPPTTimer Settings'
    [RC33Native]::PostMessage($tray,0x111,[UIntPtr]1003,[IntPtr]::Zero) | Out-Null
    $remote=Wait-Window 'Remote Control'
    Start-Sleep -Milliseconds 150
    if(-not [RC33Native]::IsWindowVisible($settings)){throw 'Opening Remote hid Settings'}
    $sizes="Settings=$([RC33Native]::Size($settings));Remote=$([RC33Native]::Size($remote))"
    foreach($window in @($settings,$remote)) {
      if([RC33Native]::SendMessage($window,0x7F,[IntPtr]::Zero,[IntPtr]::Zero) -eq [IntPtr]::Zero) {throw 'Missing small product window icon'}
    }
    [RC33Native]::PostMessage($settings,0x10,[UIntPtr]::Zero,[IntPtr]::Zero) | Out-Null
    Start-Sleep -Milliseconds 120
    if(-not [RC33Native]::IsWindowVisible($remote)){throw 'Closing Settings hid Remote'}
    [RC33Native]::PostMessage($tray,0x111,[UIntPtr]1004,[IntPtr]::Zero) | Out-Null
    $settings=Wait-Window 'FlyPPTTimer Settings'
    if(-not [RC33Native]::IsWindowVisible($remote)){throw 'Reopening Settings hid Remote'}
    [RC33Native]::PostMessage($remote,0x10,[UIntPtr]::Zero,[IntPtr]::Zero) | Out-Null
    Start-Sleep -Milliseconds 120
    if(-not [RC33Native]::IsWindowVisible($settings)){throw 'Closing Remote hid Settings'}
    [RC33Native]::PostMessage($settings,0x10,[UIntPtr]::Zero,[IntPtr]::Zero) | Out-Null
    Start-Sleep -Milliseconds 120
    $results.Add("Cycle $cycle PASS $sizes")
  }
  $results.Add('PASS: 12 cycles, both windows coexist, independent close/reopen, nonzero geometry and product icons. Does NOT prove raster correctness or every rare blank-window scenario.')
} finally {
  $tray=[RC33Native]::Find($app.Id,'FlyPPTTimerDesktopWindow',$false)
  if($tray -ne [IntPtr]::Zero){[RC33Native]::PostMessage($tray,0x111,[UIntPtr]1006,[IntPtr]::Zero) | Out-Null}
  if(-not $app.WaitForExit(8000)){$app.Kill();$results.Add('WARN: smoke process required cleanup')}
  New-Item -ItemType Directory -Force artifacts/rc33-evidence | Out-Null
  $results | Set-Content artifacts/rc33-evidence/native-smoke.txt -Encoding utf8
  $results | ForEach-Object { Write-Host $_ }
  Get-ChildItem $stage -Filter '*.log' -Recurse | Copy-Item -Destination artifacts/rc33-evidence -ErrorAction SilentlyContinue
}
