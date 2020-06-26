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
 * 形態素解析
 * https://www.nuget.org/packages/Lucene.Net.Analysis.Kuromoji/ <- これを使った
 * 
 * DataGrigViewでテーブルを作った
 *  -> わかりやすい https://dobon.net/vb/dotnet/datagridview/index.html
 * 
 * クロールのロジックはphpのロジックを流用
 * クローリングは非同期処理で実装
 * 
 * C#内部エンコーディング（UTF-16）と取得したUTF-8のXMLの違いでバグる
 * http://bbs.wankuma.com/index.cgi?mode=al2&namber=94982
 * 
 * 初期フェーズはDBを使う代わりにsqliteでメモリスタブを使う。
 * 
 * @todo
 * 正規表現でURLが削除できていない
 *  
 * @todo
 * テーブルセルを選択するとコピーできる機能がほしい
 * 
 * @todo
 * 右クリックでNGリストに追加する機能がほしい
 * 
 ***************************************/

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private SQLiteConnection sqliteConnection;
        private bool crawling = false;

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
            "しまっ",
            "いつ",
            "あれ",
            "なきゃ",
            "ところ",
            "たく",
            "ただ",
            "こっち",
            "おまえ",
            "うち",
            "ああ",
            "から",
            "おお",
            "けど",
            "ちんぽ",
            "そう",
            "って",
            "無い",
            "だけ",
            "とか",
            "いい",
            "もう",
            "まだ",
            "なんか",
            "たま",
            "まで",
            "どう",
            "さん",
            "あっ",
            "ﾋﾟｬｰ",
            "まじ",
            "ばば",
            "ねん",
            "ねえ",
            "なに",
            "でも",
            "しか",
            "ちょっと",
            "くん",
            "かも",
            "いら",
            "www",
            "ｗｗｗ",
            "やる",
            "むら",
            "まさし",
            "なんて",
            "など",
            "ぎゃあ",
            "この",
            "せろ",
            "いっ",
            "ええ",
            "あり",
            "やん",
            "おっ",
            "下さい",
            "ください",
            "について",
            "ため",
            "立て",
            "やっぱり",
            "連呼",
            "下手くそ",
            "みたい",
            "また",
            "くせ",
            "投資",
            "言っ",
            "買っ",
            "明日",
            "思う",
            "やっ",
            "今日",
            "改行",
            "マン",
            "コテ",
            "ンゴ",
            "ほぼ",
            "くらい",
            "どこ",
            "より",
            "よう",
            "やばい",
            "まぁ",
            "ぶす",
            "のに",
            "っけ",
            "そんな"
        };

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.initDbTable();
            this.initNgList();
            this.initThreadsRecords();
        }

        private delegate void DelegateShowTable();

        private void crawl()
        {
            if (this.crawling)
            {
                return;
            }

            this.crawling = true;
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
                this.crawling = false;
                this.Invoke(new DelegateShowTable(this.showTable));
                toolStripStatusLabel1.Text = "crawl done " + DateTime.Now.ToString();
            });
        }

        private void initNgList()
        {
            var sqliteCommand = new SQLiteCommand(this.sqliteConnection);
            foreach (string ngWord in this.ngList)
            {
                sqliteCommand.CommandText = "insert into ngwords (ngword) values(@ngword)";
                sqliteCommand.Parameters.Add(new SQLiteParameter("@ngword", ngWord));
                sqliteCommand.ExecuteNonQuery();
            }
        }

        private void initThreadsRecords()
        {
            var threads = this.GetThreadList();
            for (int i = 0; i < threads.GetLength(0); i++)
            {
                this.updateThread(threads[i, 0], int.Parse(threads[i, 1]), int.Parse(threads[i, 1]));
            }
        }

        private void crawlThread()
        {
            var threads = this.GetThreadList();
            for (int i = 0; i < threads.GetLength(0); i++)
            {
                if (this.existsThread(threads[i, 0]))
                {
                    var sqliteCommandForUpdate = new SQLiteCommand(this.sqliteConnection);
                    sqliteCommandForUpdate.CommandText = "update threads set count=@count where path=@path";
                    sqliteCommandForUpdate.Parameters.Add(new SQLiteParameter("@path", threads[i, 0]));
                    sqliteCommandForUpdate.Parameters.Add(new SQLiteParameter("@count", threads[i, 1]));
                    sqliteCommandForUpdate.ExecuteNonQuery();
                }
                else
                {
                    var sqliteCommandForUpdate = new SQLiteCommand(this.sqliteConnection);
                    sqliteCommandForUpdate.CommandText = "insert into threads (path,count,res) values(@path,@count,2)";
                    sqliteCommandForUpdate.Parameters.Add(new SQLiteParameter("@path", threads[i, 0]));
                    sqliteCommandForUpdate.Parameters.Add(new SQLiteParameter("@count", threads[i, 1]));
                    sqliteCommandForUpdate.ExecuteNonQuery();
                }
            }
        }


        private void crawlRes(string path, int resNo, int count)
        {
            toolStripStatusLabel1.Text = "crawl " + path;
            Thread.Sleep(5000);
            int latestResNo = resNo;
            var data = new WebClient().DownloadString("https://egg.5ch.net/test/read.cgi/stock/" + path + "/" + resNo + "-n");
            var context = BrowsingContext.New(Configuration.Default);
            var parser = context.GetService<IHtmlParser>();

            var posts = parser.ParseDocument(data).QuerySelectorAll(".post");
            foreach (var post in posts)
            {
                // 既に読み込み済みのレスならスキップ
                if (latestResNo > int.Parse(post.QuerySelectorAll(".number").First().Text())) {
                    continue;
                }

                // postの日時を取得
                var datetimeString = post.QuerySelectorAll(".date").First().Text();
                Match match = Regex.Match(datetimeString, @"^([0-9]{4}\/[0-9]{2}\/[0-9]{2}).+([0-9]{2}:[0-9]{2}:[0-9]{2})\.[0-9]{2}$");
                DateTime datetime = DateTime.Parse(match.Groups[1].Value + " " + match.Groups[2].Value);

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
                        this.insertRes(buzzword, this.getUnixTimestamp(datetime));
                    }
                }

                // 最新のレス番号を既読にセット
                latestResNo = int.Parse(post.QuerySelectorAll(".number").First().Text());

                if (latestResNo >= 1000)
                {
                    break;
                }
            }
            this.updateThread(path, count, latestResNo);
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
            sqliteCommand.CommandText = "CREATE TABLE IF NOT EXISTS ngwords(" +
                "ngword text NOT NULL PRIMARY KEY)";
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
            sqliteCommand.CommandText = "SELECT buzzword, count(buzzword) as cnt FROM res where created_timestamp > " + ((this.getUnixTimestamp(DateTime.Now)) - 300).ToString() +  " group by buzzword having count(buzzword) > 2 order by cnt desc limit 22";
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

            // NGリストに適合するか
            var sqliteCommand = new SQLiteCommand(this.sqliteConnection);
            sqliteCommand.CommandText = "SELECT count(*) as cnt from ngwords where ngword=@ngword";
            sqliteCommand.Parameters.Add(new SQLiteParameter("@ngword", buzzword));
            var reader = sqliteCommand.ExecuteReader();
            reader.Read();
            if (reader.GetInt32(0) == 1)
            {
                return true;
            }

            return false;
        }

        private void insertRes(string buzzword, int created_timestamp)
        {
            System.Diagnostics.Debug.WriteLine("buzzword" + buzzword + " t=" + created_timestamp.ToString() + " now=" + this.getUnixTimestamp(DateTime.Now));
            var sqliteCommand = new SQLiteCommand(this.sqliteConnection);
            sqliteCommand.CommandText = "INSERT INTO res (buzzword, created_timestamp) VALUES(@buzzword, @created_timestamp)";
            sqliteCommand.Parameters.Add(new SQLiteParameter("@buzzword", buzzword));
            sqliteCommand.Parameters.Add(new SQLiteParameter("@created_timestamp", created_timestamp));
            sqliteCommand.ExecuteNonQuery();
        }

        private bool existsThread(string path)
        {
            var sqliteCommand = new SQLiteCommand(this.sqliteConnection);
            sqliteCommand.CommandText = "SELECT count(*) as cnt from threads where path=" + path;
            var reader = sqliteCommand.ExecuteReader();
            reader.Read();
            if (reader.GetInt32(0) == 0)
            {
                return false;
            }
            return true;
        }

        private void updateThread(string path, int count, int resNo)
        {
            var sqliteCommand = new SQLiteCommand(this.sqliteConnection);
            var sqliteCommandForUpdate = new SQLiteCommand(this.sqliteConnection);
            sqliteCommand.CommandText = "SELECT count(*) as cnt from threads where path=" + path;
            var reader = sqliteCommand.ExecuteReader();
            reader.Read();
            if (reader.GetInt32(0) == 0)
            {
                sqliteCommandForUpdate.CommandText = "INSERT INTO threads (path, count, res_no) VALUES(@path, @count, @res_no)";
            }
            else
            {
                sqliteCommandForUpdate.CommandText = "update threads set count=@count, res_no=@res_no where path=@path";
            }
            sqliteCommandForUpdate.Parameters.Add(new SQLiteParameter("@path", path));
            sqliteCommandForUpdate.Parameters.Add(new SQLiteParameter("@count", count));
            sqliteCommandForUpdate.Parameters.Add(new SQLiteParameter("@res_no", resNo));
            sqliteCommandForUpdate.ExecuteNonQuery();
        }

        private SQLiteDataReader getThreads()
        {
            var sqliteCommand = new SQLiteCommand(this.sqliteConnection);
            var sqliteCommandForUpdate = new SQLiteCommand(this.sqliteConnection);
            sqliteCommand.CommandText = "SELECT * from threads where count > res_no and res_no < 1000";
            return sqliteCommand.ExecuteReader();
        }

        private int getUnixTimestamp(DateTime datetime)
        {
            var timespan2 = datetime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (int)timespan2.TotalSeconds;
        }


        /***********************************
         * 形態素解析
         ***********************************/
        private string[] morphologicalAnalysis(string text)
        {
            string[] keywords = { };
            var reader = new StringReader(text);
            var dictReader = new StreamReader("userdict.csv", Encoding.UTF8);
            Tokenizer tokenizer = new JapaneseTokenizer(reader, new UserDictionary(dictReader), false, JapaneseTokenizerMode.NORMAL);
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

        private void button1_Click_1(object sender, EventArgs e)
        {
//            this.truncateRes();
        }

        private void crawlTimer_Tick(object sender, EventArgs e)
        {
            this.crawl();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            this.showTable();
            this.crawl();
        }
    }
}
