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
using System.Web;
using AngleSharp.Common;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Ja;
using Lucene.Net.Analysis.Ja.Dict;
using Lucene.Net.Analysis.Ja.TokenAttributes;
using Lucene.Net.Analysis.TokenAttributes;

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
 * -> アプリ内で形態素解析したい
 * -> ローカルでやれるようになると
 *   https://www.nuget.org/packages/Lucene.Net.Analysis.Kuromoji/ <- これを使った
 * 　　・リクエスト数を気にしなくてよくなる
 * 　　　-> レスごとに形態素解析できる
 * 　　　  -> 投稿日でレスが登録できるようになる
 * 　　　    -> アーカイブの扱いに便利
 * 　　・速度向上する
 * 　　・sleep入れなくて良くなる（＝速度向上する）
 * 　　  -> 超高速になったけど403で弾かれた
 * 　　    -> proxy使えるようにする
 * 　　    -> sleep間隔を広げる（3秒ぐらい？）
 * 　　・ユーザ辞書が使えるようになる
 * 　　  -> 銘柄コード、銘柄名で分類できる
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
 *     -> つくった
 * 
 * 初期フェーズはDBを使う代わりにメモリスタブを使う。
 *  -> sqlite使ったほうが良い
 *    ->テーブル作られてない旨のエラーが出る
 *      -> テーブル作る処理を呼んでないだけだった
 * 
 * スレッド一覧をさらに分割
 * パス、スレ名、カウント->正規表現で数値だけ抜き出す
 * 
 * GitHubにリポジトリ作って保存する
 * 
 * GUIの扱い方はAndroidとほぼ同じ
 * 
 * 初回の読み込み時に全レス読み込むため時間がかかりすぎる
 * 何らかの方法（1000レス達成したスレは読まない、10分前のレスしか読まない、など）で対処したい
 * -> 非同期処理で対処した
 * 
 * 正規表現でURLが削除できていない
 * 
 * 差分のクロールがうまくいっていない
 * 
 * 前回から更新のないスレッドはクロールしないようにする
 * 
 * クローリングは非同期処理にしたい
 * -> method名asyncにしただけでは非同期処理にならない？
 *   -> 実装した
 *   
 * タイマ処理でクローリング、テーブル表示処理をそれぞれバックグラウンドで行いたい
 * 
 * テーブルセルを選択するとコピーできる機能がほしい
 * 
 ***************************************/

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private SQLiteConnection sqliteConnection;
        private int getResFrom = 180;
        private string[] ngList =
        {
            "ない",
            "オッパ",
            "てる",
            "こと",
            "だろ",
            "たら",
            "する",
            "たい",
            "スレ",
            "レス",
            "です",
            "なら",
            "なっ",
            "これ",
            "れる",
            "ます",
            "てん",
            "お前",
            "コドージ",
            "クソ",
            "せる",
            "ます",
            "キチガイ",
            "だっ",
            "やろ",
            "ここ",
            "じゃ",
            "それ",
            "やつ",
            "なかっ",
            "いる",
            "もの",
            "なん",
            "なる",
            "なく",
            "まし",
            "でしょ",
            "マジ",
            "でる",
            "はぶ",
            "ちゃう",
            "すぎ",
            "くれ",
            "とけ",
            "そこ",
            "くる",
            "える",
            "られ",
            "アホ",
            "モー",
            "らしい",
            "とき",
            "できる",
            "でき",
            "すぎる",
            "ある",
            "あと",
            "NG",
            "坂井",
            "ホモ",
            "わけ",
            "まとも",
            "ねー",
            "とこ",
            "たく",
            "しまっ",
            "いつ",
            "あれ",
            "なきゃ",
            "ところ",
            "たく",
            "ただ",
            "こっち",
            "おまえ",
            "うち"
        };

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.initDbTable();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            Task task = Task.Run(() =>
            {
                this.crawlThread();
                var reader = this.getThreads();
                while (reader.Read())
                {
                    crawlRes(
                        reader.GetString(0), //path
                        reader.GetInt32(1),  //res_no
                        reader.GetInt32(1)); //count
                }
            });
        }

        private void crawlThread()
        {
            toolStripStatusLabel1.Text = "crawl thread";
            var threads = this.GetThreadList();
            for (int i = 0; i < threads.GetLength(0); i++)
            {
                this.updateThread(threads[i, 0], threads[i, 1]);
            }
        }
        private void crawlRes(string path, int resNo, int count)
        {
            toolStripStatusLabel1.Text = "crawl " + path;
            Thread.Sleep(5000);
            int latestResNo = resNo;
            var data = new WebClient().DownloadString("https://egg.5ch.net/test/read.cgi/stock/" + path);
            var context = BrowsingContext.New(Configuration.Default);
            var parser = context.GetService<IHtmlParser>();

            var posts = parser.ParseDocument(data).QuerySelectorAll(".post");
            bool firstRes = true;
            foreach (var post in posts)
            {
                // 既に読み込み済みのレスならスキップ
                if (latestResNo > int.Parse(post.QuerySelectorAll(".number").First().Text())) {
                    continue;
                }

                var message = post.QuerySelectorAll(".message").First().Text();

                // URLを削除
                message = System.Text.RegularExpressions.Regex.Replace(
                    message, @"^s?https?://[-_.!~*'()a-zA-Z0-9;/?:@&=+$,%#]+$@", "");

                // レスアンカーを削除
                message = System.Text.RegularExpressions.Regex.Replace(
                    message, @">>[0-9]+?", "");

                // 空白、改行を削除
                message = message.Replace(" ", "").Replace("　", "").Replace("\r", "");

                var words = this.morphologicalAnalysis(message);
                foreach (var buzzword in words)
                {
                    if (!this.isExcludeWord(buzzword))
                    {
                        this.insertRes(buzzword);
                    }
                }

                latestResNo++;

                if (latestResNo >= 1000)
                {
                    break;
                }
            }

            var sqliteCommand = new SQLiteCommand(this.sqliteConnection);
            sqliteCommand.CommandText = "update threads set res_no=@res_no where path=@path";
            sqliteCommand.Parameters.Add(new SQLiteParameter("@path", path));
            sqliteCommand.Parameters.Add(new SQLiteParameter("@res_no", latestResNo.ToString()));
            sqliteCommand.ExecuteNonQuery();
        }

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

            string[,] threads = new string[i, 3];

            var j = 0;
            foreach (var item in Items)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(
                    item.Text(), @"今買えばいい株.*\([0-9]+\)$"))
                {
                    string[] arr = item.GetAttribute("href").Split('/');
                    threads[j, 0] = arr[0]; // これでurlが取れる
                    Match match = Regex.Match(item.Text(), @"\(([0-9]+)\)$");
                    threads[j, 1] = match.Groups[1].Value; // これでカウントが取れる
                    j++;
                }
            }
            return threads;
        }
        private void initDbTable()
        {
            var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = ":memory:" };
            this.sqliteConnection = new SQLiteConnection(sqlConnectionSb.ToString());
            this.sqliteConnection.Open();
            var sqliteCommand = new SQLiteCommand(this.sqliteConnection);
            //テーブル作成
            sqliteCommand.CommandText = "CREATE TABLE IF NOT EXISTS threads(" +
                "path text NOT NULL primary key," +
                "res_no integer NOT NULL," +
                "count INTEGER NOT NULL)";
            sqliteCommand.ExecuteNonQuery();
            sqliteCommand.CommandText = "CREATE TABLE IF NOT EXISTS res(" +
                "id integer NOT NULL PRIMARY KEY," +
                "buzzword text NOT NULL," +
                "created_timestamp  integer NOT NULL)";
            sqliteCommand.ExecuteNonQuery();

            sqliteCommand.CommandText = "insert into res(buzzword, created_timestamp) values('res'," + this.getUnixTimestamp() + ")";
            sqliteCommand.ExecuteNonQuery();
        }

        private void showTable()
        {
            // DataGridView初期化（データクリア）
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            dataGridView1.ColumnCount = 2;
            dataGridView1.Columns[0].HeaderText = "単語";
            dataGridView1.Columns[1].HeaderText = "カウント";


            var sqliteCommand = new SQLiteCommand(this.sqliteConnection);
            sqliteCommand.CommandText = "SELECT buzzword, count(buzzword) as cnt FROM res where created_timestamp > @from group by buzzword having count(buzzword) > 3 order by cnt desc";
            sqliteCommand.Parameters.Add(new SQLiteParameter("@from", this.getUnixTimestamp() - this.getResFrom));
            var reader = sqliteCommand.ExecuteReader();
            while (reader.Read())
            {
                dataGridView1.Rows.Add(reader[0], reader[1]);
            }
        }

        private bool isExcludeWord(string buzzword)
        {
            // 一文字の場合は漢字以外は除外
            if (buzzword.Length == 1 && !Regex.IsMatch(buzzword, @"^[\u3402-\uFA6D]+$@"))
            {
                return true;
            }

            foreach (string ngWord in ngList)
            {
                if (ngWord == buzzword)
                {
                    return true;
                }
            }

            return false;
        }

        private void insertRes(string buzzword)
        {
            var sqliteCommand = new SQLiteCommand(this.sqliteConnection);
            sqliteCommand.CommandText = "INSERT INTO res (buzzword, created_timestamp) VALUES(@buzzword, @created_timestamp)";
            sqliteCommand.Parameters.Add(new SQLiteParameter("@buzzword", buzzword));
            sqliteCommand.Parameters.Add(new SQLiteParameter("@created_timestamp", this.getUnixTimestamp()));
            sqliteCommand.ExecuteNonQuery();
        }

        private void updateThread(string path, string count)
        {
            var sqliteCommand = new SQLiteCommand(this.sqliteConnection);
            var sqliteCommandForUpdate = new SQLiteCommand(this.sqliteConnection);
            sqliteCommand.CommandText = "SELECT count(*) as cnt from threads where path=" + path;
            var reader = sqliteCommand.ExecuteReader();
            reader.Read();
            if (reader.GetInt32(0) == 0)
            {
                sqliteCommandForUpdate.CommandText = "INSERT INTO threads (path, count, res_no) VALUES(@path, @count, 2)";
            }
            else
            {
                sqliteCommandForUpdate.CommandText = "update threads set count=@count where path=@path";
            }
            sqliteCommandForUpdate.Parameters.Add(new SQLiteParameter("@path", path));
            sqliteCommandForUpdate.Parameters.Add(new SQLiteParameter("@count", count));
            sqliteCommandForUpdate.ExecuteNonQuery();
        }

        private SQLiteDataReader getThreads()
        {
            var sqliteCommand = new SQLiteCommand(this.sqliteConnection);
            var sqliteCommandForUpdate = new SQLiteCommand(this.sqliteConnection);
            sqliteCommand.CommandText = "SELECT * from threads where count > res_no and res_no < 1000";
            return sqliteCommand.ExecuteReader();
        }

        private uint getUnixTimestamp()
        {
            var timespan2 = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (uint)timespan2.TotalSeconds;
        }


        private void btnGetRes_Click(object sender, EventArgs e)
        {
            this.showTable();
        }
        
        /***********************************
         * 形態素解析
         ***********************************/
        private string[] morphologicalAnalysis(string text)
        {
            string[] keywords = { };
            var reader = new StringReader(text);
            Tokenizer tokenizer = new JapaneseTokenizer(reader, null, false, JapaneseTokenizerMode.NORMAL);
            var tokenStreamComponents = new TokenStreamComponents(tokenizer, tokenizer);
            using (var tokenStream = tokenStreamComponents.TokenStream)
            {
                // note:処理の実行前にResetを実行する必要がある
                tokenStream.Reset();

                while (tokenStream.IncrementToken())
                {
                    Array.Resize(ref keywords, keywords.Length + 1);
                    keywords[keywords.Length - 1] = tokenStream.GetAttribute<ICharTermAttribute>().ToString();
                }
            }

            return keywords;
        }
    }
}
