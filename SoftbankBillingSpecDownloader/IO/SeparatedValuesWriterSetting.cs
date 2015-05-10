using System;



namespace SoftbankBillingSpecDownloader.IO
{
    /// <summary>
    /// 区切り文字形式での書き込み機能の設定を表します。
    /// </summary>
    public class SeparatedValuesWriterSetting
    {
        #region 一般的な形式のインスタンス取得
        /// <summary>
        /// CSV形式で書き込むための設定を取得します。
        /// </summary>
        public static SeparatedValuesWriterSetting Csv{ get { return new SeparatedValuesWriterSetting(); } }


        /// <summary>
        /// TSV形式で書き込むための設定を取得します。
        /// </summary>
        public static SeparatedValuesWriterSetting Tsv{ get { return new SeparatedValuesWriterSetting() { FieldSeparator = "\t" }; } }


        /// <summary>
        /// SSV形式で書き込むための設定を取得します。
        /// </summary>
        public static SeparatedValuesWriterSetting Ssv{ get { return new SeparatedValuesWriterSetting() { FieldSeparator = " " }; } }
        #endregion


        #region プロパティ
        /// <summary>
        /// フィールドの区切り文字を取得または設定します。
        /// </summary>
        public string FieldSeparator{ get; set; }


        /// <summary>
        /// レコードの区切り文字を取得または設定します。
        /// </summary>
        public string RecordSeparator{ get; set; }


        /// <summary>
        /// テキストの修飾子を取得または設定します。
        /// </summary>
        public string TextModifier{ get; set; }
        #endregion


        #region コンストラクタ
        /// <summary>
        /// 既定の設定でインスタンスを生成します。
        /// </summary>
        public SeparatedValuesWriterSetting()
        {
            //--- 既定はCSV
            this.FieldSeparator     = ",";
            this.RecordSeparator    = Environment.NewLine;
            this.TextModifier       = "\"";
        }
        #endregion
    }
}