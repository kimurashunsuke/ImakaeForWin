using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Xml.Linq;
using System.Xml;
using System.Globalization;
using System.Threading;
using System.Text.RegularExpressions;
using System.Data.SQLite;
using AngleSharp.Text;

/***************************************
 * 
 * 行ったこと
 * NuGetでパッケージAngleSharpをインストール
 * AngleSharpはスクレイピングできる。
 * https://anglesharp.github.io/
 * NuGetは右ペインのソリューションエクスプローラーの
 * ソリューション名を右クリックで呼び出せる。
 * 
 * 正規表現はPHPのpreg関数で使ったものがそのまま使えた。
 * ロジックもそのまま流用。
 * デスクトップにPHPファイルを保存しているので参照。
 * 
 * 形態素解析はとりあえずPHP版そのままにYahooAPIを利用
 * １日５万件までなので連続多用注意
 * 
 * bodyが大きいと413エラー payload to largeになる
 * 文字列の分割を行うがうまくいかない？
 * 文字エンコーディングが関係か
 * どうせ将来はメカブで言語処理するのでpostするのはありかもしれん
 *  -> 配列は初期化時に配列数の定義が必須だった、これを省略していたので例外になっていた
 * 
 * @todo:
 * C#内部エンコーディング（UTF-16）と取得したUTF-8のXMLの違いでバグる
 * http://bbs.wankuma.com/index.cgi?mode=al2&namber=94982
 * 
 * DataTableでリストを作る。
 *  -> リスト表示するのはDataTableではない、DataGrigViewが正しい
 *  -> わかりやすい https://dobon.net/vb/dotnet/datagridview/index.html
 * 
 * 初期フェーズはDBを使う代わりにメモリスタブを使う。
 *  -> sqlite使ったほうが良い
 *    ->テーブル作られてない旨のエラーが出る
 * 
 * スレッド一覧をさらに分割
 * パス、スレ名、カウント->正規表現で数値だけ抜き出す
 * 
 * GitHubにリポジトリ作って保存する
 * 
 * GUIの扱い方はAndroidとほぼ同じ
 * 
 ***************************************/

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void initDbTable()
        {
            var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = ":memory:" };
            using (var cn = new SQLiteConnection(sqlConnectionSb.ToString()))
            {
                cn.Open();

                using (var cmd = new SQLiteCommand(cn))
                {
                    //テーブル作成
                    cmd.CommandText = "CREATE TABLE IF NOT EXISTS threads(" +
                        "path text NOT NULL PRIMARY KEY," +
                        "title TEXT NOT NULL," +
                        "count INTEGER NOT NULL)";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "CREATE TABLE IF NOT EXISTS res(" +
                        "buzzword text NOT NULL PRIMARY KEY," +
                        "created_timestamp  integer NOT NULL)";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "insert into res(buzzword, created_timestamp) values('res'," + this.getUnixTimestamp() + ")";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void insertRes(string buzzword)
        {
            var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = ":memory:" };
            using (var cn = new SQLiteConnection(sqlConnectionSb.ToString()))
            {
                cn.Open();

                using (var cmd = new SQLiteCommand(cn))
                {
                    cmd.CommandText = "insert into res(buzzword, created_timestamp) values('" + buzzword + "'," + this.getUnixTimestamp() + ")";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private uint getUnixTimestamp()
        {
            var timespan2 = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (uint)timespan2.TotalSeconds;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var threads = this.GetThreadList();

            // DataGridView初期化（データクリア）
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            dataGridView1.ColumnCount = 3;
            dataGridView1.Columns[0].HeaderText = "スレ名";
            dataGridView1.Columns[1].HeaderText = "path";
            dataGridView1.Columns[2].HeaderText = "カウント";
            for (int i = 0; i < threads.GetLength(0); i ++)
            {
                dataGridView1.Rows.Add(threads[i, 0], threads[i, 1], threads[i, 2]);
            }
        }

        ///
        /// スレッド一覧
        ///
        private string[,] GetThreadList()
        {
            // 全スレ一覧取得
            var data = new WebClient().DownloadString("https://egg.5ch.net/stock/subback.html");
            var context = BrowsingContext.New(Configuration.Default);
            var parser = context.GetService<IHtmlParser>();
            var Items = parser.ParseDocument(data).QuerySelectorAll("#trad a");


            var i = 0;
            foreach (var item in Items)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(
                    item.Text(), @"今買えばいい株.*\(([0-9]+)\)$"))
                {
                    i++;
                }
            }

            string[,] threads = new string[i,3];

            var j = 0;
            foreach (var item in Items)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(
                    item.Text(), @"今買えばいい株.*\([0-9]+\)$"))
                {
                    // @todo カウントも取る
                    threads[j, 0] = item.Text(); // これでテキストが取れる
                    threads[j, 1] = item.GetAttribute("href"); // これでurlが取れる
                    Match match = Regex.Match(item.Text(), @"\(([0-9]+)\)$");
                    threads[j, 2] = match.Value; // これでカウントが取れる
                    j++;
                }

                /*
                                Regex rgx = new Regex(@"今買えばいい株.*\(([0-9]+)\)$");
                                MatchCollection matches = rgx.Matches(item.Text());
                                if (matches.Count > 0)
                                {
                                    // @todo カウントも取る
                                    threads[j, 0] = item.Text(); // これでテキストが取れる
                                    threads[j, 1] = item.GetAttribute("href"); // これでurlが取れる
                                    foreach (Match match in matches)
                                    {
                                        threads[j, 2] = match.Value; // これでカウントが取れる
                                    }
                                    j++;
                                }
                */


                /*
                if (System.Text.RegularExpressions.Regex.IsMatch(
                    item.Text(), @"今買えばいい株.*\(([0-9]+)\)$"))
                {
                    // @todo カウントも取る
                    threads[j,0] = item.Text(); // これでテキストが取れる
                    threads[j,1] = item.GetAttribute("href"); // これでurlが取れる
                    j++;
                }
                */
            }
            return threads;
        }

        private void btnGetRes_Click(object sender, EventArgs e)
        {
            var data = new WebClient().DownloadString("https://matsuri.5ch.net/test/read.cgi/morningcoffee/1591527265/");
            var context = BrowsingContext.New(Configuration.Default);
            var parser = context.GetService<IHtmlParser>();

            var posts = parser.ParseDocument(data).QuerySelectorAll(".post");
            string text = "";
            foreach (var post in posts)
            {
                var message = post.QuerySelectorAll(".message").First().Text();
                
                // URLを削除
                message = System.Text.RegularExpressions.Regex.Replace(
                    message, @"@(https?://([-\w\.]+[-\w])+(:\d+)?(/([\w/_\.#-]*(\?\S+)?[^\.\s])?)?)@", "");

                // レスアンカーを削除
                message = System.Text.RegularExpressions.Regex.Replace(
                    message, @">>[0-9]+?", "");

                text += message; // これでテキストが取れる
            }

            /*
            StringInfo si = new StringInfo(text);
            MessageBox.Show(si.LengthInTextElements + "");
            return;
            */
            int maxLen = 1000;
            string[] arrText = new String[(int)Math.Ceiling((decimal)text.Length / maxLen)];
            for (int i = 0; (i * maxLen) < text.Length; i ++)
            {
                int len = maxLen;
                if (text.Length < (i + 1) * maxLen)
                {
                    len = text.Length - (i * maxLen);
                }
                arrText[i] = text.Substring(i * maxLen, len);
                //                Console.WriteLine(arrText[i]);
//                MessageBox.Show(arrText[i]);
            }

            // 形態素解析
            foreach (string splitText in arrText)
            {
                var response = this.morphologicalAnalysis(splitText);

                // XMLパース
                XDocument xdoc = XDocument.Parse(response);
                XNamespace df = xdoc.Root.Name.Namespace;
                var elements = from c in xdoc.Descendants(df + "word")
                               select c;
                //            resultTextBox.Text = elements.Count() + "\r\n";
                foreach (var element in elements)
                {
                    for (var i = 0; i < int.Parse(element.Element(df + "count").Value); i ++)
                    {
                        this.insertRes(element.Element(df + "surface").Value);
                    }
//                    string row = element.Element(df + "surface").Value + "(" + element.Element(df + "count").Value + ")" + "\r\n";
                    //                resultTextBox.Text += row;
                }
            }
        }
        
        /***********************************
         * 形態素解析
         ***********************************/
        private string morphologicalAnalysis(string text)
        {
            string postData = "results=uniq&appid=dj0zaiZpPU9XbHVHYWRiSmFDVCZzPWNvbnN1bWVyc2VjcmV0Jng9NmE-&sentence=" + text;
            var webClient = new WebClient();
            webClient.Encoding = System.Text.Encoding.UTF8;
            webClient.Headers["Content-Type"] = "application/x-www-form-urlencoded";
            var xml = webClient.UploadString("https://jlp.yahooapis.jp/MAService/V1/parse", "POST", postData);
            return xml;
        }
    }
}
