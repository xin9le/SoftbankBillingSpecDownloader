using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SoftbankBillingSpecDownloader.Models;
using Xceed.Wpf.Toolkit;



namespace SoftbankBillingSpecDownloader
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        #region コンストラクタ
        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            #region ボタン制御
            var userIdChanged   = Observable.FromEventPattern(this.userIdBox,   nameof(TextBox.TextChanged));
            var passwordChanged = Observable.FromEventPattern(this.passwordBox, nameof(PasswordBox.PasswordChanged));
            var dateChanged     = Observable.FromEventPattern(this.datePicker,  nameof(DateTimeUpDown.ValueChanged));
            userIdChanged
                .Select(_ => this.userIdBox.Text)
                .CombineLatest
                (
                    passwordChanged.Select(_ => this.passwordBox.Password),
                    dateChanged.Select(_ => this.datePicker.Value),
                    (userId, password, month) =>
                    {
                        return  !string.IsNullOrWhiteSpace(userId)
                            &&  !string.IsNullOrWhiteSpace(password)
                            &&  month.HasValue;
                    }
                )
                .Subscribe(x => this.downloadButton.IsEnabled = x);
            #endregion

            #region ダウンロード
            Observable.FromEventPattern(this.downloadButton, nameof(Button.Click))
                .Select(x =>
                {
                    var date = this.datePicker.Value.Value;
                    var dialog = new SaveFileDialog();
                    dialog.DefaultExt = "*.csv";
                    dialog.Filter = "CSV (カンマ区切り)|*.csv";
                    dialog.OverwritePrompt = true;
                    dialog.RestoreDirectory = false;
                    dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    dialog.FileName = $"請求明細 {date.ToString("yyyy-MM")}";
                    return dialog.ShowDialog(this) != true ? null : new
                    {
                        UserId   = this.userIdBox.Text,
                        Password = this.passwordBox.Password,
                        Date     = date,
                        FilePath = dialog.FileName,
                    };
                })
                .Where(x => x != null)
                .Subscribe(async x =>
                {
                    try
                    {
                        this.busyIndicator.IsBusy = true;
                        using (var client = new BillingSpecClient())
                        {
                            await client.SigninAsync(x.UserId, x.Password);
                            var breakdowns = await client.DownloadBreakdownAsync(x.Date);
                            await BillingBreakdown.From(breakdowns).SaveAsync(x.FilePath);
                        }
                        this.busyIndicator.IsBusy = false;
                    }
                    catch
                    {
                        this.busyIndicator.IsBusy = false;
                        System.Windows.MessageBox.Show("ダウンロード処理中にエラーが発生しました。", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            #endregion

            #region 初期値
            this.datePicker.Value = DateTime.Today;
            #endregion
        }
        #endregion
    }
}