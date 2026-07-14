using System.Diagnostics; using FlyPPTTimer.Core.Services; using Microsoft.UI.Xaml; using Microsoft.UI.Xaml.Controls; using Microsoft.UI.Xaml.Media.Imaging; using Windows.ApplicationModel.DataTransfer; using Windows.Storage; using Windows.Storage.Pickers;
namespace FlyPPTTimer.WinUI.Pages;
public sealed partial class FilesPage : Page
{
    private MainViewModel Vm => App.Services.ViewModel;
    public FilesPage(){InitializeComponent();DataContext=Vm;}
    private async void Add_Click(object s,RoutedEventArgs e){var picker=new FileOpenPicker();InitPicker(picker);picker.FileTypeFilter.Add(".ppt");picker.FileTypeFilter.Add(".pptx");picker.FileTypeFilter.Add(".pptm");var files=await picker.PickMultipleFilesAsync();ShowErrors(Vm.AddFiles(files.Select(x=>x.Path)));}
    private async void Delete_Click(object s,RoutedEventArgs e){if(!Vm.Rules.Any(x=>x.IsSelected)){Show("请先选择文件",InfoBarSeverity.Warning);return;}var d=new ContentDialog{Title="删除所选规则？",Content="不会删除磁盘上的 PPT 文件。",PrimaryButtonText="删除",CloseButtonText="取消",XamlRoot=XamlRoot};if(await d.ShowAsync()==ContentDialogResult.Primary&&!Vm.RemoveSelected(out var error))Show(error,InfoBarSeverity.Error);}
    private async void ClearAll_Click(object s,RoutedEventArgs e){if(Vm.Rules.Count==0)return;var d=new ContentDialog{Title="清空全部文件规则？",Content="不会删除磁盘上的 PPT 文件；正在放映的文件规则会受到保护。",PrimaryButtonText="清空",CloseButtonText="取消",DefaultButton=ContentDialogButton.Close,XamlRoot=XamlRoot};if(await d.ShowAsync()==ContentDialogResult.Primary&&!Vm.RemoveAll(out var error))Show(error,InfoBarSeverity.Error);}
    private void SelectAll_Click(object s,RoutedEventArgs e){foreach(var x in Vm.Rules)x.IsSelected=true;} private void ClearSelection_Click(object s,RoutedEventArgs e){foreach(var x in Vm.Rules)x.IsSelected=false;}
    private void Top_Click(object s,RoutedEventArgs e)=>Vm.MoveSelected(MoveDirection.Top); private void Up_Click(object s,RoutedEventArgs e)=>Vm.MoveSelected(MoveDirection.Up); private void Down_Click(object s,RoutedEventArgs e)=>Vm.MoveSelected(MoveDirection.Down); private void Bottom_Click(object s,RoutedEventArgs e)=>Vm.MoveSelected(MoveDirection.Bottom);
    private void Duration_LostFocus(object s,RoutedEventArgs e){if(s is TextBox {DataContext:FileRuleRowViewModel row}){if(!TimeSpan.TryParseExact(row.Duration,@"hh\:mm\:ss",null,out var value)||value<=TimeSpan.Zero){Show("时长必须使用 HH:mm:ss 且大于零",InfoBarSeverity.Error);row.Duration=row.Model.Duration;}else Vm.UpdateRule(row);}}
    private void Enabled_Toggled(object s,RoutedEventArgs e){if(s is ToggleSwitch {DataContext:FileRuleRowViewModel row})Vm.UpdateRule(row);}
    private async void Open_Click(object s,RoutedEventArgs e){if(Vm.SelectedRule is not null)await Vm.OpenRule(Vm.SelectedRule,false);} private async void OpenStart_Click(object s,RoutedEventArgs e){if(Vm.SelectedRule is not null)await Vm.OpenRule(Vm.SelectedRule,true);} private async void OpenCurrent_Click(object s,RoutedEventArgs e){if(Vm.SelectedRule is not null)await Vm.OpenRule(Vm.SelectedRule,true,true);}
    private void Explorer_Click(object s,RoutedEventArgs e){var path=Vm.SelectedRule?.FilePath;if(!string.IsNullOrWhiteSpace(path))Process.Start(new ProcessStartInfo("explorer.exe",$"/select,\"{path}\""){UseShellExecute=true});}
    private void CopyPath_Click(object s,RoutedEventArgs e){if(Vm.SelectedRule is null)return;var p=new DataPackage();p.SetText(Vm.SelectedRule.FilePath);Clipboard.SetContent(p);Show("路径已复制",InfoBarSeverity.Success);}
    private async void Relocate_Click(object s,RoutedEventArgs e){var picker=new FileOpenPicker();InitPicker(picker);picker.FileTypeFilter.Add(".ppt");picker.FileTypeFilter.Add(".pptx");picker.FileTypeFilter.Add(".pptm");var file=await picker.PickSingleFileAsync();if(file is not null&&!Vm.RelocateSelected(file.Path,out var error))Show(error,InfoBarSeverity.Error);}
    private void BatchDuration_Click(object s,RoutedEventArgs e){if(!Vm.SetSelectedDuration(BatchDuration.Text))Show("批量时长无效，请使用 HH:mm:ss",InfoBarSeverity.Error);else Show("已更新所选文件时长",InfoBarSeverity.Success);}
    private void Page_DragOver(object s,DragEventArgs e){e.AcceptedOperation=DataPackageOperation.Copy;e.DragUIOverride.Caption="添加到 PPT 文件列表";}
    private void RuleList_DragItemsCompleted(ListViewBase sender,DragItemsCompletedEventArgs args)=>Vm.SaveVisibleRuleOrder();
    private async void RuleList_SelectionChanged(object sender,SelectionChangedEventArgs e)
    {
        ThumbnailImage.Source=null;ThumbnailPlaceholder.Text="正在生成预览...";ThumbnailPlaceholder.Visibility=Visibility.Visible;
        if(Vm.SelectedRule is null)return;
        var path=await Vm.GenerateThumbnail(Vm.SelectedRule);
        if(string.IsNullOrWhiteSpace(path)){ThumbnailPlaceholder.Text="无法生成首张幻灯片预览";return;}
        ThumbnailImage.Source=new BitmapImage(new Uri(path));ThumbnailPlaceholder.Visibility=Visibility.Collapsed;
    }
    private async void Page_Drop(object s,DragEventArgs e){if(!e.DataView.Contains(StandardDataFormats.StorageItems))return;var items=await e.DataView.GetStorageItemsAsync();ShowErrors(Vm.AddFiles(items.OfType<StorageFile>().Select(x=>x.Path)));}
    private void InitPicker(object picker){var hwnd=WinRT.Interop.WindowNative.GetWindowHandle(App.Services.MainWindow);WinRT.Interop.InitializeWithWindow.Initialize(picker,hwnd);}
    private void ShowErrors(IReadOnlyList<string> errors){if(errors.Count>0)Show(string.Join(Environment.NewLine,errors),InfoBarSeverity.Warning);}
    private void Show(string message,InfoBarSeverity severity){MessageBar.Message=message;MessageBar.Severity=severity;MessageBar.IsOpen=true;}
}
