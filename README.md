# Softbank 請求明細ダウンローダー

[Softbank WEB 明細](https://web-meisai.softbanktelecom.co.jp/cgi-bin/meisai/usr/scripts/web_meisai.jsp) にある請求内訳データを自動でダウンロードするツール。



## 作成理由

明細の CSV はひとつのファイルで成っているのではなく、Web ページごとに別のファイルとしてダウンロードする仕様。それぞれを結合することで本来の明細が得られる。ページ数が多いとダウンロード + ファイル結合の手間がバカにならないため作成。



## License

This library is provided under [MIT License](http://opensource.org/licenses/MIT).



## Author

Takaaki Suzuki (a.k.a [@xin9le](https://twitter.com/xin9le)) is software developer in Japan who awarded Microsoft MVP for .NET since July 2012.
