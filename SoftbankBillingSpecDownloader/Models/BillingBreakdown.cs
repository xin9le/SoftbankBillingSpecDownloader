using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using SoftbankBillingSpecDownloader.IO;
using This = SoftbankBillingSpecDownloader.Models.BillingBreakdown;



namespace SoftbankBillingSpecDownloader.Models
{
    /// <summary>
    /// 請求料金内訳を表します。
    /// </summary>
    public class BillingBreakdown
    {
        #region プロパティ
        /// <summary>
        /// お客様番号を取得します。
        /// </summary>
        public string CustomerNumber { get; }


        /// <summary>
        /// 請求書発行番号を取得します。
        /// </summary>
        public string PublishNumber { get; }


        /// <summary>
        /// 請求月を取得します。
        /// </summary>
        public DateTime BillingDate { get; }


        /// <summary>
        /// 内訳データを取得します。
        /// </summary>
        public IReadOnlyCollection<string[]> Data { get; }
        #endregion


        #region コンストラクタ
        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <param name="customerNumber">お客様番号</param>
        /// <param name="publishNumber">請求書発行番号</param>
        /// <param name="billingDate">請求月</param>
        /// <param name="data">内訳データ</param>
        public BillingBreakdown(string customerNumber, string publishNumber, DateTime billingDate, IReadOnlyCollection<string[]> data)
        {
            if (customerNumber == null) throw new ArgumentNullException(nameof(customerNumber));
            if (publishNumber == null)  throw new ArgumentNullException(nameof(publishNumber));
            if (data == null)           throw new ArgumentNullException(nameof(data));

            this.CustomerNumber = customerNumber;
            this.PublishNumber = publishNumber;
            this.BillingDate = billingDate;
            this.Data = data;
        }
        #endregion


        #region 生成
        /// <summary>
        /// 分割された請求内訳データからインスタンスを生成します。
        /// </summary>
        /// <param name="partials">分割された内訳データのコレクション</param>
        /// <returns>請求内訳データ</returns>
        public static This From(IReadOnlyCollection<PartialBillingBreakdown> partials)
        {
            if (partials == null)   throw new ArgumentNullException(nameof(partials));
            if (!partials.Any())    throw new ArgumentException("要素数が0です。");

            var query   = partials
                        .OrderBy(x => x.Number)
                        .Select(x =>
                        {
                            var records = This.EnumerateRecords(x.Data).ToArray();
                            return new
                            {
                                Order           = x.Number,
                                CustomerNumber  = records.First(y => y[0] == "お客様番号")[1],
                                PublishNumber   = records.First(y => y[0] == "請求書発行番号")[1],
                                BillingDate     = x.Date,
                                Breakdowns      = records
                                                .Where(y => y.Length == 9)
                                                .Select((y, i) =>
                                                {
                                                    var add = (i == 0) ? "ページ番号" : x.Number.ToString();
                                                    return new [] { add }.Concat(y).ToArray();
                                                }),
                            };
                        })
                        .ToArray();
            var data    = query
                        .SelectMany(x => x.Breakdowns)
                        .Where((x, i) => i == 0 || x[0] != "ページ番号")
                        .ToArray();
            return new This
            (
                query.First().CustomerNumber,
                query.First().PublishNumber,
                query.First().BillingDate,
                data
            );
        }


        /// <summary>
        /// 指定されたCSVに含まれるフィールドの一覧を取得します。
        /// </summary>
        /// <param name="csv">CSVデータ</param>
        /// <returns>フィールドのコレクション</returns>
        private static IEnumerable<string[]> EnumerateRecords(byte[] csv)
        {
            using (var stream = new MemoryStream(csv))
            using (var parser = new TextFieldParser(stream, Encoding.GetEncoding("shift-jis")))
            {
                parser.SetDelimiters(",");
                parser.TrimWhiteSpace = true;
                parser.TextFieldType = FieldType.Delimited;
                while (!parser.EndOfData)
                    yield return parser.ReadFields();
            }
        }
        #endregion


        #region 保存
        /// <summary>
        /// 指定されたパスにファイルを保存します。
        /// </summary>
        /// <param name="path">保存先フォルダパス</param>
        /// <returns>処理</returns>
        public async Task SaveAsync(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            using (var writer = new SeparatedValuesWriter(path, SeparatedValuesWriterSetting.Csv))
            {
                foreach (var x in this.Data)
                    await writer.WriteLineAsync(x).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }
        #endregion
    }
}