using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace SoftbankBillingSpecDownloader.IO
{
    /// <summary>
    /// 区切り文字形式での書き込み機能を提供します。
    /// </summary>
    public class SeparatedValuesWriter : IDisposable
    {
        #region フィールド / プロパティ
        /// <summary>
        /// 書き込み機能を保持します。
        /// </summary>
        private readonly TextWriter writer = null;


        /// <summary>
        /// 区切り文字形式での書き込み設定を取得します。
        /// </summary>
        public SeparatedValuesWriterSetting Setting{ get; private set; }
        #endregion


        #region コンストラクタ
        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <param name="path">ファイルの書き込み先の絶対パス</param>
        /// <param name="setting">書き込み設定</param>
        public SeparatedValuesWriter(string path, SeparatedValuesWriterSetting setting)
            : this(path, Encoding.UTF8, setting)
        {}


        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <param name="path">ファイルの書き込み先の絶対パス</param>
        /// <param name="encoding">エンコーディング</param>
        /// <param name="setting">書き込み設定</param>
        public SeparatedValuesWriter(string path, Encoding encoding, SeparatedValuesWriterSetting setting)
            : this(new StreamWriter(path, false, encoding), setting)
        {}


        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <param name="path">ファイルの書き込み先の絶対パス</param>
        /// <param name="append">既存ファイルの末尾に追加書き込みするか</param>
        /// <param name="setting">書き込み設定</param>
        public SeparatedValuesWriter(string path, bool append, SeparatedValuesWriterSetting setting)
            : this(path, append, Encoding.UTF8, setting)
        {}


        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <param name="path">ファイルの書き込み先の絶対パス</param>
        /// <param name="append">既存ファイルの末尾に追加書き込みするか</param>
        /// <param name="encoding">エンコーディング</param>
        /// <param name="setting">書き込み設定</param>
        public SeparatedValuesWriter(string path, bool append, Encoding encoding, SeparatedValuesWriterSetting setting)
            : this(new StreamWriter(path, append, encoding), setting)
        {}


        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <param name="stream">書き込み先のストリーム</param>
        /// <param name="setting">書き込み設定</param>
        public SeparatedValuesWriter(Stream stream, SeparatedValuesWriterSetting setting)
            : this(stream, Encoding.UTF8, setting)
        {}


        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <param name="stream">書き込み先のストリーム</param>
        /// <param name="encoding">エンコーディング</param>
        /// <param name="setting">書き込み設定</param>
        public SeparatedValuesWriter(Stream stream, Encoding encoding, SeparatedValuesWriterSetting setting)
            : this(new StreamWriter(stream, encoding), setting)
        {}


        /// <summary>
        /// インスタンスを生成します。
        /// </summary>
        /// <param name="writer">書き込み機能</param>
        /// <param name="setting">書き込み設定</param>
        public SeparatedValuesWriter(TextWriter writer, SeparatedValuesWriterSetting setting)
        {
            this.writer  = writer;
            this.Setting = setting;
        }
        #endregion


        #region IDisposable メンバー
        /// <summary>
        /// 使用したリソースを破棄します。
        /// </summary>
        public void Dispose()
        {
            this.Close();
        }
        #endregion


        #region 書き込み関連メソッド
        /// <summary>
        /// 行終端記号を書き込みます。
        /// </summary>
        public void WriteLine()
        {
            this.writer.WriteLine();
        }


        /// <summary>
        /// 1行分のデータを書き込みます。
        /// </summary>
        /// <typeparam name="T">データ型</typeparam>
        /// <param name="fields">書き込むデータのコレクション</param>
        /// <param name="quoteAlways">データを常に引用符で括るかどうか</param>
        public void WriteLine<T>(IEnumerable<T> fields, bool quoteAlways = false)
        {
            this.WriteLineAsync(fields, quoteAlways).Wait();
        }


        /// <summary>
        /// 行終端記号を書き込みます。
        /// </summary>
        public Task WriteLineAsync()
        {
            return this.writer.WriteLineAsync();
        }


        /// <summary>
        /// 1行分のデータを非同期に書き込みます。
        /// </summary>
        /// <typeparam name="T">データ型</typeparam>
        /// <param name="fields">書き込むデータのコレクション</param>
        /// <param name="quoteAlways">データを常に引用符で括るかどうか</param>
        public Task WriteLineAsync<T>(IEnumerable<T> fields, bool quoteAlways = false)
        {
            if (fields == null)
                throw new ArgumentNullException("fields");

            var formated    = fields.Select(x => this.FormatField(x, quoteAlways));
            var record      = string.Join(this.Setting.FieldSeparator, formated);
            return this.writer.WriteAsync(record + this.Setting.RecordSeparator);
        }


        /// <summary>
        /// 現在のライターのすべてのバッファーをクリアし、バッファー内のデータをデバイスに書き込みます。
        /// </summary>
        public void Flush()
        {
            this.writer.Flush();
        }


        /// <summary>
        /// 現在のライターのすべてのバッファーを非同期にクリアし、バッファー内のデータをデバイスに書き込みます。
        /// </summary>
        public Task FlushAsync()
        {
            return this.writer.FlushAsync();
        }


        /// <summary>
        /// 現在のライターを終了し、ライターに関連付けられたすべてのシステムリソースを開放します。
        /// </summary>
        public void Close()
        {
            this.writer.Close();
        }
        #endregion


        #region 補助メソッド
        /// <summary>
        /// フィールド文字列を整形します。
        /// </summary>
        /// <typeparam name="T">フィールドデータの型</typeparam>
        /// <param name="field">フィールドデータ</param>
        /// <param name="quoteAlways">常に引用符で括るかどうか</param>
        /// <returns>整形された文字列</returns>
        private string FormatField<T>(T field, bool quoteAlways = false)
        {
            var text    = field is string ? field as string
                        : field == null ? null
                        : field.ToString();
            text        = text ?? string.Empty;

            if (quoteAlways || this.NeedsQuote(text))
            {
                var modifier = this.Setting.TextModifier;
                var escape = modifier + modifier;
                var builder = new StringBuilder(text);
                builder.Replace(modifier, escape);
                builder.Insert(0, modifier);
                builder.Append(modifier);
                return builder.ToString();
            }
            return text;
        }

        //--- 指定された文字列を引用符で括る必要があるかどうかを判定
        private bool NeedsQuote(string text)
        {
            return text.Contains('\r')
                || text.Contains('\n')
                || text.Contains(this.Setting.TextModifier)
                || text.Contains(this.Setting.FieldSeparator)
                || text.StartsWith("\t")
                || text.StartsWith(" ")
                || text.EndsWith("\t")
                || text.EndsWith(" ");
        }
        #endregion
    }
}