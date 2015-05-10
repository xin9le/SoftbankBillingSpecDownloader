using System;



namespace SoftbankBillingSpecDownloader.Models
{
    /// <summary>
    /// 分割された請求内訳データの一部を表します。
    /// </summary>
    public class PartialBillingBreakdown
    {
        #region プロパティ
        /// <summary>
        /// 請求月を取得します。
        /// </summary>
        public DateTime Date { get; }


        /// <summary>
        /// 分割番号を取得します。
        /// </summary>
        public int Number { get; }


        /// <summary>
        /// 内訳の生データを取得します。
        /// </summary>
        public byte[] Data { get; }
        #endregion


        #region コンストラクタ
        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <param name="date">請求月</param>
        /// <param name="number">内訳番号</param>
        /// <param name="data">生データ</param>
        public PartialBillingBreakdown(DateTime date, int number, byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            this.Date = date;
            this.Number = number;
            this.Data = data;
        }
        #endregion
    }
}