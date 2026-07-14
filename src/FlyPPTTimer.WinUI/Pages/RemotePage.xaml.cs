using System.Diagnostics; using Microsoft.UI.Xaml; using Microsoft.UI.Xaml.Controls; using Microsoft.UI.Xaml.Media.Imaging; using QRCoder; using Windows.ApplicationModel.DataTransfer; using Windows.Storage.Streams;
namespace FlyPPTTimer.WinUI.Pages;
public sealed partial class RemotePage : Page
{
    public RemotePage(){InitializeComponent();DataContext=App.Services.ViewModel;AddressBox.ItemsSource=App.Services.Network.GetIPv4Addresses();AddressBox.DisplayMemberPath="Address";AddressBox.SelectedIndex=0;Loaded+=async(_,_)=>await RefreshQr();}
    private async Task RefreshQr(string? selectedUrl=null){var url=selectedUrl??App.Services.ViewModel.RemoteUrl;using var gen=new QRCodeGenerator();using var data=gen.CreateQrCode(url,QRCodeGenerator.ECCLevel.Q);var bytes=new PngByteQRCode(data).GetGraphic(8);var stream=new InMemoryRandomAccessStream();using(var writer=new DataWriter(stream)){writer.WriteBytes(bytes);await writer.StoreAsync();}stream.Seek(0);var image=new BitmapImage();await image.SetSourceAsync(stream);QrImage.Source=image;UrlBox.Text=url;}
    private async void Restart_Click(object s,RoutedEventArgs e){App.Services.ViewModel.RestartRemote();await RefreshQr();} private async void Disconnect_Click(object s,RoutedEventArgs e){App.Services.ViewModel.DisconnectAll();await RefreshQr();}
    private async void Address_Changed(object s,SelectionChangedEventArgs e){if(AddressBox.SelectedItem is FlyPPTTimer.Core.Models.NetworkAddressInfo item){var port=App.Services.Remote.CurrentPort;var url=$"http://{item.Address}:{port}/?token={App.Services.Controller.Config.RemoteControl.Token}";await RefreshQr(url);}}
    private void Copy_Click(object s,RoutedEventArgs e){var p=new DataPackage();p.SetText(UrlBox.Text);Clipboard.SetContent(p);} private void Open_Click(object s,RoutedEventArgs e)=>Process.Start(new ProcessStartInfo(UrlBox.Text){UseShellExecute=true});
    private void Firewall_Click(object s,RoutedEventArgs e){var command=$"netsh advfirewall firewall add rule name=\"FlyPPTTimer\" dir=in action=allow protocol=TCP localport={App.Services.Remote.CurrentPort}";var p=new DataPackage();p.SetText(command);Clipboard.SetContent(p);}
}
