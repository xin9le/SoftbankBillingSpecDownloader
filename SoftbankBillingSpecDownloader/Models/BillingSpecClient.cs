using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Sgml;
using SoftbankBillingSpecDownloader.Extensions;
using This = SoftbankBillingSpecDownloader.Models.BillingSpecClient;



namespace SoftbankBillingSpecDownloader.Models
{
    /// <summary>
    /// WEB明細へアクセスする機能を提供します。
    /// </summary>
    public class BillingSpecClient : IDisposable
    {
        #region プロパティ
        /// <summary>
        /// HTTP通信を行うための機能を取得します。
        /// </summary>
        private HttpClient Client { get; } = new HttpClient();
        #endregion


        #region コンストラクタ
        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        public BillingSpecClient()
        {}
        #endregion


        #region サインイン
        /// <summary>
        /// 指定されたユーザーIDとパスワードでサインインします。
        /// </summary>
        /// <param name="userId">ユーザーID</param>
        /// <param name="password">パスワード</param>
        /// <returns>処理</returns>
        public async Task SigninAsync(string userId, string password)
        {
            await this.CacheWebSpecCookieAsync().ConfigureAwait(false);
            await this.EmulateSigninStep1Async(userId, password).ConfigureAwait(false);
            await this.EmulateSigninStep2Async(userId, password).ConfigureAwait(false);
            await this.CachePriceGuidanceCookieAsync().ConfigureAwait(false);
        }


        /// <summary>
        /// WEB明細サイトのCookieをキャッシュします。
        /// </summary>
        /// <returns>処理</returns>
        private async Task CacheWebSpecCookieAsync()
        {
            var uri = new Uri("https://web-meisai.softbanktelecom.co.jp/cgi-bin/meisai/usr/scripts/web_meisai.jsp");
            var response = await this.Client.GetAsync(uri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }


        /// <summary>
        /// 第1段階のサインインを模倣します。
        /// </summary>
        /// <param name="userId">ユーザーID</param>
        /// <param name="password">パスワード</param>
        /// <returns>処理</returns>
        private async Task EmulateSigninStep1Async(string userId, string password)
        {
            var formData = new Dictionary<string, string>
            {
                ["hidPageID"]       = string.Empty,
                ["@base"]           = string.Empty,
                ["hidNext_page"]    = string.Empty,
                ["hidCountNG"]      = "0",
                ["hidPreUser_id"]   = string.Empty,
                ["hidReadchk"]      = "1",
                ["txtUser_id"]      = userId,
                ["pasPassword"]     = password,
            };
            using (var content = new FormUrlEncodedContent(formData))
            {
                var uri = new Uri("https://web-meisai.softbanktelecom.co.jp/cgi-bin/meisai/usr/scripts/login.jsp");
                var response = await this.Client.PostAsync(uri, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
        }


        /// <summary>
        /// 第2段階のサインインを模倣します。
        /// </summary>
        /// <param name="userId">ユーザーID</param>
        /// <param name="password">パスワード</param>
        /// <returns>処理</returns>
        private async Task EmulateSigninStep2Async(string userId, string password)
        {
            var formData = new Dictionary<string, string>
            {
                ["hidPageID"]       = string.Empty,
                ["base"]            = "login.jsp",
                ["hidNext_page"]    = "top_usr.jsp",
                ["hidCountNG"]      = "0",
                ["hidPreUser_id"]   = userId,
                ["txtUser_id"]      = userId,
                ["pasPassword"]     = password,
                ["BV_UseBVCookie"]  = "NO",
            };
            using (var content = new FormUrlEncodedContent(formData))
            {
                var uri = new Uri("https://web-meisai.softbanktelecom.co.jp/cgi-bin/meisai/usr/scripts/WC102001.jsp");
                var response = await this.Client.PostAsync(uri, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
        }


        /// <summary>
        /// オンライン料金案内のCookieをキャッシュします。
        /// </summary>
        /// <returns>処理</returns>
        private async Task CachePriceGuidanceCookieAsync()
        {
            IReadOnlyDictionary<string, string> formData = null;
            {
                //--- オンライン料金案内のリンク先を取得
                var url = "https://web-meisai.softbanktelecom.co.jp/cgi-bin/meisai/usr/scripts/obi_redirect.jsp";
                var response = await this.Client.GetAsync(url).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                //--- htmlを取得してリダイレクト用のパラメーターを取得
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    formData = This.ParseHtml(reader)
                                .Descendants("input")
                                .Select(x => new
                                {
                                    Name = x.Attribute("name").Value,
                                    Value = x.Attribute("value").Value,
                                })
                                .ToDictionary(x => x.Name, x => x.Value);
                }
            }

            //--- リダイレクト先にPOSTしてCookieを取得
            using (var content = new FormUrlEncodedContent(formData))
            {
                var uri = new Uri("https://bltm11.my.softbank.jp/wcot/index/");
                var response = await this.Client.PostAsync(uri, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
        }
        #endregion


        #region ダウンロード
        /// <summary>
        /// 指定された年月の内訳データをダウンロードします。
        /// </summary>
        /// <param name="month">対象年月</param>
        /// <returns>内訳データ</returns>
        public async Task<IReadOnlyCollection<PartialBillingBreakdown>> DownloadBreakdownAsync(DateTime month)
        {
            var last = await this.GetLastPageNumber(month).ConfigureAwait(false);
            return  await Enumerable.Range(1, last)
                    .Select(async x => new PartialBillingBreakdown
                    (
                        month,
                        x,
                        await this.DownloadBreakdownAsync(month, x).ConfigureAwait(false)
                    ))
                    .WhenAll()
                    .ConfigureAwait(false);
        }


        /// <summary>
        /// 指定された年月の最終ページ番号を取得します。
        /// </summary>
        /// <param name="month">対象年月</param>
        /// <returns>最終ページ番号</returns>
        private async Task<int> GetLastPageNumber(DateTime month)
        {
            //--- 料金明細内訳のトップページを取得
            var billYm = $"?billYm={month.ToString("yyyyMM")}";
            var uri = new Uri($"https://bltm11.my.softbank.jp/wcot/billTotal/doBillItems{billYm}");
            var response = await this.Client.GetAsync(uri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            //--- スクレイピングして最終ページを取得
            using (var reader = new StreamReader(stream, Encoding.GetEncoding(932)))
            {
                var @namespace = XNamespace.Get("http://www.w3.org/1999/xhtml");
                var max = This.ParseHtml(reader)
                        .Descendants(@namespace + "p")
                        .SingleOrDefault(x => x.Attribute("class")?.Value == "pagelink")
                        ?.Elements(@namespace + "a")
                        .Select(x => x.Attribute("href"))
                        .Where(x => x != null)
                        .Select(x => x.Value)
                        .Select(x => x.Replace("/wcot/billItems/goPaging/", string.Empty))
                        .Select(x => x.Replace(billYm, string.Empty))
                        .Select(x =>
                        {
                            int value = 0;
                            return int.TryParse(x, out value) ? value : (int?)null;
                        })
                        .Max();
                return max ?? 1;
            }
        }


        /// <summary>
        /// 指定された年月、ページ番号の内訳データをダウンロードします。
        /// </summary>
        /// <param name="month">対象年月</param>
        /// <param name="pageNumber">対象ページ番号</param>
        /// <returns>内訳データ</returns>
        private async Task<byte[]> DownloadBreakdownAsync(DateTime month, int pageNumber)
        {
            var date = month.ToString("yyyyMM");
            var uri = new Uri($"https://bltm11.my.softbank.jp/wcot/billItems/doCsv?billYm={date}&pageNumber={pageNumber}");
            var response = await this.Client.GetAsync(uri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }
        #endregion


        #region 補助
        /// <summary>
        /// 指定された読み込み機能で読み込まれるHTMLを解析します。
        /// </summary>
        /// <param name="reader">読み込み機能</param>
        /// <returns>解析結果のHTML</returns>
        private static XDocument ParseHtml(TextReader reader)
        {
            using (var sgml = new SgmlReader() { DocType = "HTML", CaseFolding = CaseFolding.ToLower, IgnoreDtd = true })
            {
                sgml.InputStream = reader;
                return XDocument.Load(sgml);
            }
        }
        #endregion


        #region IDisposable Support
        /// <summary>
        /// 使用されたリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            this.Client.Dispose();
        }
        #endregion
    }
}