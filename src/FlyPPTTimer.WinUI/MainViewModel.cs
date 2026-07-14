using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlyPPTTimer.Core.Models;
using FlyPPTTimer.Core.Services;
using Microsoft.UI.Dispatching;

namespace FlyPPTTimer.WinUI;

public partial class MainViewModel : ObservableObject
{
    private readonly AppServices _services;
    private DispatcherQueue? _dispatcher;
    [ObservableProperty] private string timerDisplay = "08:00";
    [ObservableProperty] private string timerStatus = "停止";
    [ObservableProperty] private string timerMode = "倒计时";
    [ObservableProperty] private string presentationName = "未打开演示文稿";
    [ObservableProperty] private string presentationPath = "";
    [ObservableProperty] private string slideText = "0 / 0";
    [ObservableProperty] private string presentationStatus = "未放映";
    [ObservableProperty] private string screenMode = "正常";
    [ObservableProperty] private string remoteStatus = "未启动";
    [ObservableProperty] private string remoteUrl = "";
    [ObservableProperty] private string lastMessage = "准备就绪";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private FileRuleRowViewModel? selectedRule;
    public ObservableCollection<FileRuleRowViewModel> Rules { get; } = [];
    public ObservableCollection<PresentationOption> Presentations { get; } = [];
    public AppConfig Config => _services.Controller.Config;
    public IReadOnlyList<string> RecentLogs => _services.Log.ReadRecent();

    public MainViewModel(AppServices services) { _services = services; _services.Controller.State.Changed += (_, state) => Dispatch(() => ApplyState(state)); }
    public void AttachDispatcher(DispatcherQueue dispatcher) => _dispatcher = dispatcher;
    public void RefreshAll() { RefreshRules(); ApplyState(_services.Controller.State.Current); }
    private void Dispatch(Action action) { if (_dispatcher is null || _dispatcher.HasThreadAccess) action(); else _dispatcher.TryEnqueue(() => action()); }
    private void ApplyState(ApplicationState state)
    {
        TimerDisplay = state.Timer.DisplayText; TimerStatus = StateText(state.Timer.State); TimerMode = state.Timer.Mode == Core.Models.TimerMode.Countdown ? "倒计时" : "正计时";
        PresentationName = string.IsNullOrWhiteSpace(state.Presentation.PresentationName) ? "未打开演示文稿" : state.Presentation.PresentationName;
        PresentationPath = state.Presentation.PresentationPath; SlideText = $"{state.Presentation.CurrentSlide} / {state.Presentation.TotalSlides}";
        PresentationStatus = state.Presentation.IsSlideShowRunning ? "正在放映" : state.Presentation.Error.Length > 0 ? "连接异常" : "未放映"; ScreenMode = state.Presentation.ScreenMode;
        Presentations.Clear(); foreach (var item in state.Presentation.Presentations) Presentations.Add(item);
        RemoteStatus = _services.Remote.IsRunning ? $"已启动 · {state.ConnectedClients} 台设备" : "未启动"; RemoteUrl = BuildRemoteUrl();
        LastMessage = string.IsNullOrWhiteSpace(state.LastMessage) ? "准备就绪" : state.LastMessage;
    }
    private string BuildRemoteUrl() { var address = _services.Network.GetIPv4Addresses().FirstOrDefault(x => x.Recommended)?.Address ?? "127.0.0.1"; var port = _services.Remote.CurrentPort > 0 ? _services.Remote.CurrentPort : Config.RemoteControl.Port; return $"http://{address}:{port}/?token={Config.RemoteControl.Token}"; }
    private static string StateText(Core.Models.TimerState state) => state switch { Core.Models.TimerState.Running => "运行中", Core.Models.TimerState.Paused => "暂停", Core.Models.TimerState.Finished => "已结束", _ => "停止" };

    [RelayCommand] private void StartTimer() => _services.Controller.ExecuteTimer("timer.start");
    [RelayCommand] private void PauseTimer() => _services.Controller.ExecuteTimer("timer.pause");
    [RelayCommand] private void ResumeTimer() => _services.Controller.ExecuteTimer("timer.resume");
    [RelayCommand] private void StopTimer() => _services.Controller.ExecuteTimer("timer.stop");
    [RelayCommand] private void ToggleTimer() => _services.Controller.ExecuteTimer("timer.toggle");
    [RelayCommand] private async Task PptCommand(string? command) { if (string.IsNullOrWhiteSpace(command) || IsBusy) return; IsBusy = true; try { await _services.Controller.ExecutePresentationAsync(new(command)); } finally { IsBusy = false; } }
    public async Task OpenPresentation(string id, bool start, bool current = false) { if (IsBusy) return; IsBusy = true; try { var result = await _services.Controller.ExecutePresentationAsync(new("ppt.openPresentation", id)); if (result.Success && start) await _services.Controller.ExecutePresentationAsync(new(current ? "ppt.startFromCurrent" : "ppt.startFromBeginning")); } finally { IsBusy = false; } }
    public async Task OpenRule(FileRuleRowViewModel row, bool start, bool current = false)
    {
        var option = _services.Controller.State.Current.Presentation.Presentations.FirstOrDefault(x => string.Equals(Path.Combine(x.Directory, x.Name), row.Model.FilePath, StringComparison.OrdinalIgnoreCase));
        if (option is null) { LastMessage = row.Model.Exists ? "PowerPoint 列表正在刷新，请稍后重试" : "文件不存在，请先重新定位"; return; }
        await OpenPresentation(option.Id, start, current);
    }
    public async Task<string?> GenerateThumbnail(FileRuleRowViewModel row)
    {
        var option = _services.Controller.State.Current.Presentation.Presentations.FirstOrDefault(x => FileRuleService.SamePath(Path.Combine(x.Directory, x.Name), row.Model.FilePath));
        if (option is null || !row.Model.Exists) return null;
        return await _services.PowerPoint.GenerateFirstSlideThumbnailAsync(option.Id, FlyPPTTimer.Infrastructure.Windows.AppPaths.CacheDirectory);
    }
    public async Task GoToSlide(int slide) { if (slide <= 0) return; await _services.Controller.ExecutePresentationAsync(new("ppt.gotoSlide", SlideNumber: slide)); }

    public IReadOnlyList<string> AddFiles(IEnumerable<string> paths) { var errors = _services.Controller.Rules.AddFiles(paths); _services.Controller.SaveRules(); RefreshRules(); return errors; }
    public bool RemoveSelected(out string error) { var result = _services.Controller.Rules.Remove(Rules.Where(x => x.IsSelected).Select(x => x.Model.Id), _services.Controller.State.Current.Presentation.PresentationPath, out error); if (result) { _services.Controller.SaveRules(); RefreshRules(); } return result; }
    public bool RemoveAll(out string error)
    {
        foreach (var row in Rules) row.IsSelected = true;
        return RemoveSelected(out error);
    }
    public bool SetSelectedDuration(string duration) { var result = _services.Controller.Rules.SetDuration(Rules.Where(x => x.IsSelected).Select(x => x.Model.Id), duration); if (result) { _services.Controller.SaveRules(); RefreshRules(); } return result; }
    public void MoveSelected(MoveDirection direction) { if (SelectedRule is null) return; _services.Controller.Rules.Move(SelectedRule.Model.Id, direction); _services.Controller.SaveRules(); RefreshRules(SelectedRule.Model.Id); }
    public bool RelocateSelected(string path, out string error) { if (SelectedRule is null) { error = "未选择文件。"; return false; } var id = SelectedRule.Model.Id; var ok = _services.Controller.Rules.Relocate(id, path, out error); if (ok) { _services.Controller.SaveRules(); RefreshRules(id); } return ok; }
    public void UpdateRule(FileRuleRowViewModel row) { row.Model.Enabled = row.Enabled; row.Model.Duration = row.Duration; _services.Controller.SaveRules(); }
    public void SaveVisibleRuleOrder()
    {
        _services.Controller.Rules.SetOrder(Rules.Select(x => x.Model.Id));
        _services.Controller.SaveRules();
        RefreshRules(SelectedRule?.Model.Id);
    }
    public void RefreshRules(string? selectId = null)
    {
        var presentation = _services.Controller.State.Current.Presentation;
        Rules.Clear();
        foreach (var item in _services.Controller.Rules.Rules) Rules.Add(new(item, presentation));
        SelectedRule = Rules.FirstOrDefault(x => x.Model.Id == selectId) ?? Rules.FirstOrDefault();
    }
    public void ApplyConfig(AppConfig config) { _services.Controller.ApplyConfig(config); _services.Remote.Restart(); _services.ConfigureHotkeys(); _services.Overlays?.Rebuild(); OnPropertyChanged(nameof(Config)); RefreshAll(); }
    public void DisconnectAll() { _services.Remote.DisconnectAll(); RefreshAll(); }
    public void RestartRemote() { _services.Remote.Restart(); RefreshAll(); }
    public void RestartPowerPoint() => _ = _services.PowerPoint.RestartAsync();
}

public partial class FileRuleRowViewModel : ObservableObject
{
    public FileRuleRowViewModel(FileRule model, PresentationState presentation)
    {
        Model = model; enabled = model.Enabled; duration = model.Duration;
        var option = presentation.Presentations.FirstOrDefault(x => FileRuleService.SamePath(Path.Combine(x.Directory, x.Name), model.FilePath));
        Status = !model.Exists ? "文件不存在" : option?.IsSlideShow == true ? "正在放映" : option?.IsActive == true ? "当前" : option?.IsOpen == true ? "已打开" : "待打开";
    }
    public FileRule Model { get; }
    public string FileName => Model.FileName;
    public string FilePath => Model.FilePath;
    public string Directory => Path.GetDirectoryName(Model.FilePath) ?? "";
    public string FileSize => Model.Exists ? $"{Model.FileSize / 1024d / 1024d:0.##} MB" : "-";
    public string Modified => Model.LastModified?.ToString("yyyy-MM-dd HH:mm") ?? "-";
    public string Position => $"第 {Model.Order + 1} 项";
    public string Status { get; }
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private bool enabled;
    [ObservableProperty] private string duration;
}
