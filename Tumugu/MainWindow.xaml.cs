using Markdig;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Tumugu
{
    public partial class MainWindow : Window
    {
        // 現在の保存先フォルダと編集中のファイル名を保持するフィールド
        private string _currentEditFolder;
        private string _currentEditFileName;

        public MainWindow()
        {
            InitializeComponent();

            // キャプションバー以外でもドラッグ可能にする
            this.MouseLeftButtonDown += (sender, e) => this.DragMove();

            // WebView2 の初期化と、ドラッグオーバー・ドロップイベントの無効化スクリプトの登録
            InitializeWebView();

            // WPF の WebView2 はブラウザ内部の右クリックを直接 WPF 側で拾えないため、CoreWebView2 のイベントを使って右クリックを検出します。
            MarkdownBrowser.CoreWebView2InitializationCompleted += MarkdownBrowser_CoreWebView2InitializationCompleted;

            // 初期化完了イベントで UI を切り替える初期状態では WebView2 を非表示にしておき、初期化完了後に表示する（これで真っ白な空白が一瞬見えるのを防止）
            MarkdownBrowser.Visibility = Visibility.Hidden;

            // 現在のモニタに合わせた作業領域を取得
            // 最大化時のサイズを制限（これでタスクバーを隠さない）
            Rect workArea = ScreenHelper.GetCurrentWorkArea(this);
            this.MaxWidth = workArea.Width;
            this.MaxHeight = workArea.Height;

            this.Topmost = true;
            this.Topmost = false;

            _currentEditFolder = @"C:\Temp";
        }

        private async void InitializeWebView()
        {
            await MarkdownBrowser.EnsureCoreWebView2Async();

            MarkdownBrowser.NavigateToString(@"
                    <!DOCTYPE html>
                    <html>
                    <head> <meta charset=""""utf-8""""> <base href=""""https://local.example/""""> </head>
                    <style>
                    /* ===== Markdown Base Style ===== */
                    body {{
                        font-family: -apple-system, BlinkMacSystemFont, """"Segoe UI"""", Helvetica, Arial, sans-serif;
                        font-size: 16px;
                        line-height: 1.6;
                        color: #24292e;
                        background: #ffffff;
                        padding: 20px;
                    }}

                    /* Headings */
                    h1, h2, h3, h4, h5, h6 {{
                        font-weight: 600;
                        margin-top: 24px;
                        margin-bottom: 16px;
                        line-height: 1.25;
                    }}
                    h1 {{ font-size: 2em; border-bottom: 1px solid #eaecef; padding-bottom: .3em; }}
                    h2 {{ font-size: 1.5em; border-bottom: 1px solid #eaecef; padding-bottom: .3em; }}
                    h3 {{ font-size: 1.25em; }}
                    h4 {{ font-size: 1em; }}
                    h5 {{ font-size: .875em; }}
                    h6 {{ font-size: .85em; color: #6a737d; }}

                    /* Paragraph */
                    p {{
                        margin: 16px 0;
                    }}

                    /* Links */
                    a {{
                        color: #0366d6;
                        text-decoration: none;
                    }}
                    a:hover {{
                        text-decoration: underline;
                    }}

                    /* Lists */
                    ul, ol {{
                        padding-left: 2em;
                        margin: 16px 0;
                    }}
                    li {{
                        margin: 4px 0;
                    }}

                    /* Blockquote */
                    blockquote {{
                        padding: 0 1em;
                        color: #6a737d;
                        border-left: .25em solid #dfe2e5;
                        margin: 16px 0;
                    }}

                    /* Code (inline) */
                    code {{
                        background-color: rgba(27,31,35,.05);
                        padding: .2em .4em;
                        border-radius: 3px;
                        font-family: SFMono-Regular, Consolas, """"Liberation Mono"""", Menlo, monospace;
                        font-size: 85%;
                    }}

                    /* Code block */
                    pre {{
                        background-color: #f6f8fa;
                        padding: 16px;
                        border-radius: 6px;
                        overflow: auto;
                    }}
                    pre code {{
                        background: none;
                        padding: 0;
                        font-size: 85%;
                    }}

                    /* Table */
                    table {{
                        border-collapse: collapse;
                        width: 100%;
                        margin: 16px 0;
                    }}
                    th, td {{
                        border: 1px solid #dfe2e5;
                        padding: 6px 13px;
                    }}
                    th {{
                        background: #f6f8fa;
                        font-weight: 600;
                    }}
                    tr:nth-child(even) {{
                        background: #fafbfc;
                    }}

                    /* Horizontal rule */
                    hr {{
                        border: 0;
                        border-top: 1px solid #eaecef;
                        margin: 24px 0;
                    }}

                    /* Images */
                    img {{
                        max-width: 100%;
                        height: auto;
                    }}

                    /* Task list */
                    .task-list-item {{
                        list-style-type: none;
                    }}
                    .task-list-item input {{
                        margin-right: .5em;
                    }}
                    </style>

                    </head>
                    <body>
                    <div id='content'></div>
                    </body>
                    </html>
                    ");

            // ページロード時および遷移時に常に実行されるスクリプトを登録
            await MarkdownBrowser.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                    window.addEventListener('dragover', function(e) {
                        e.preventDefault();
                        e.dataTransfer.dropEffect = 'none'; // カーソルを禁止にする
                    }, false);

                    window.addEventListener('drop', function(e) {
                        e.preventDefault(); // ドロップ動作を無効化
                    }, false);
                ");
        }

        private void LblTitleBlankArea_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ChangeWindowStage();

            ChangeMarkdownTextBoxWidth(999);
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            ChangeWindowStage();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ChangeWindowStage()
        {
            // WPFで WindowState = WindowState.Maximized; にした際、通常はOSがタスクバー（下のメニュー）を避けて最大化してくれます。
            // しかし、WindowStyle = "None" を指定してカスタムウィンドウを作っている場合、タスクバーを覆い隠してフルスクリーンになってしまうというWPF特有の挙動があります。これをWindows 11のタスクバーを考慮したサイズにするには、主に2つの方法があります。
            this.WindowState = this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        private void MarkdownBrowser_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            MarkdownBrowser.Visibility = Visibility.Visible;

            // 右クリックイベントをフック
            MarkdownBrowser.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;
        }

        private void CoreWebView2_ContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            e.Handled = true; // 標準メニューを消す

            var menu = new System.Windows.Controls.ContextMenu();

            var printItem = new System.Windows.Controls.MenuItem();
            printItem.Header = "Markdownとして印刷";
            printItem.Click += (s, ev) =>
            {
                MarkdownBrowser.CoreWebView2.ShowPrintUI();
            };

            menu.Items.Add(printItem);

            // 右クリック位置に表示
            menu.IsOpen = true;
        }

        private async void RewriteMarkdownBrowser()
        {
            if (MarkdownBrowser == null) return;

            // Markdown → HTML Markdig を使う場合の変換
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string plainText = GetPlainTextFromRichTextBox(RichMarkdownTextBox);
            var htmlBody = Markdown.ToHtml(plainText, pipeline);

            await MarkdownBrowser.ExecuteScriptAsync($@"
                document.getElementById('content').innerHTML = `{htmlBody}`;
                ");

            // 最低限の CSS を付与（白基調・読みやすい
            var htmlTemplate = $@"
                    <!DOCTYPE html>
                    <html>
                    <head> <meta charset=""utf-8""> <base href=""https://local.example/""> </head>
                    <style>
                    /* ===== Markdown Base Style ===== */
                    body {{
                        font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Helvetica, Arial, sans-serif;
                        font-size: 16px;
                        line-height: 1.6;
                        color: #24292e;
                        background: #ffffff;
                        padding: 20px;
                    }}

                    /* Headings */
                    h1, h2, h3, h4, h5, h6 {{
                        font-weight: 600;
                        margin-top: 24px;
                        margin-bottom: 16px;
                        line-height: 1.25;
                    }}
                    h1 {{ font-size: 2em; border-bottom: 1px solid #eaecef; padding-bottom: .3em; }}
                    h2 {{ font-size: 1.5em; border-bottom: 1px solid #eaecef; padding-bottom: .3em; }}
                    h3 {{ font-size: 1.25em; }}
                    h4 {{ font-size: 1em; }}
                    h5 {{ font-size: .875em; }}
                    h6 {{ font-size: .85em; color: #6a737d; }}

                    /* Paragraph */
                    p {{
                        margin: 16px 0;
                    }}

                    /* Links */
                    a {{
                        color: #0366d6;
                        text-decoration: none;
                    }}
                    a:hover {{
                        text-decoration: underline;
                    }}

                    /* Lists */
                    ul, ol {{
                        padding-left: 2em;
                        margin: 16px 0;
                    }}
                    li {{
                        margin: 4px 0;
                    }}

                    /* Blockquote */
                    blockquote {{
                        padding: 0 1em;
                        color: #6a737d;
                        border-left: .25em solid #dfe2e5;
                        margin: 16px 0;
                    }}

                    /* Code (inline) */
                    code {{
                        background-color: rgba(27,31,35,.05);
                        padding: .2em .4em;
                        border-radius: 3px;
                        font-family: SFMono-Regular, Consolas, ""Liberation Mono"", Menlo, monospace;
                        font-size: 85%;
                    }}

                    /* Code block */
                    pre {{
                        background-color: #f6f8fa;
                        padding: 16px;
                        border-radius: 6px;
                        overflow: auto;
                    }}
                    pre code {{
                        background: none;
                        padding: 0;
                        font-size: 85%;
                    }}

                    /* Table */
                    table {{
                        border-collapse: collapse;
                        width: 100%;
                        margin: 16px 0;
                    }}
                    th, td {{
                        border: 1px solid #dfe2e5;
                        padding: 6px 13px;
                    }}
                    th {{
                        background: #f6f8fa;
                        font-weight: 600;
                    }}
                    tr:nth-child(even) {{
                        background: #fafbfc;
                    }}

                    /* Horizontal rule */
                    hr {{
                        border: 0;
                        border-top: 1px solid #eaecef;
                        margin: 24px 0;
                    }}

                    /* Images */
                    img {{
                        max-width: 100%;
                        height: auto;
                    }}

                    /* Task list */
                    .task-list-item {{
                        list-style-type: none;
                    }}
                    .task-list-item input {{
                        margin-right: .5em;
                    }}
                    </style>

                    </head>
                    <body>
                    <div id='content'>{htmlBody}</div>
                    </body>
                    </html>
                    ";

            // WebView2 に HTML を表示
            await MarkdownBrowser.EnsureCoreWebView2Async();

            MarkdownBrowser.CoreWebView2.SetVirtualHostNameToFolderMapping("local.example", @"C:\Develop\WpfStartSample\WpfStartSample\Doc", CoreWebView2HostResourceAccessKind.Allow);
            MarkdownBrowser.CoreWebView2.NavigateToString(htmlTemplate);
        }

        private void MarkdownBrowser_DragOver(object sender, DragEventArgs e)
        {
            // ドラッグ操作を拒否し、禁止カーソルを表示させる
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void ChangeMarkdownTextBoxWidth(double ratio)
        {
            // WindowタイトルのlblTitleBlankAreaをダブルクリックで最大化/元に戻すとき、ここに999が渡されるようにしている
            if (ratio == 999) 
            {
                return;
            }

            if (ratio == 4)
            {
                TextBoxColumn.Width = new GridLength(ratio, GridUnitType.Star);
                MarkdownBrowserColumn.Width = new GridLength(0, GridUnitType.Star);
            }
            else
            {
                // MarkdownTextBoxColumnの幅を変更
                TextBoxColumn.Width = new GridLength(ratio, GridUnitType.Star);
                MarkdownBrowserColumn.Width = new GridLength(3, GridUnitType.Star);
            }
        }

        private void Btn0Percent_Click(object sender, RoutedEventArgs e)
        {
            ChangeMarkdownTextBoxWidth(0);
        }

        private void Btn25Percent_Click(object sender, RoutedEventArgs e)
        {
            ChangeMarkdownTextBoxWidth(1);
        }

        private void Btn50Percent_Click(object sender, RoutedEventArgs e)
        {
            ChangeMarkdownTextBoxWidth(3);
        }

        private void Btn100Percent_Click(object sender, RoutedEventArgs e)
        {
            ChangeMarkdownTextBoxWidth(4);
        }

        private bool _isChanging = false; // 無限ループ防止フラグ

        // RichTextBox からプレーンテキストを取得する
        private string GetPlainTextFromRichTextBox(RichTextBox rtb)
        {
            // RichTextドキュメントの全範囲(ContentStartからContentEndまで)を指定
            TextRange textRange = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);

            return textRange.Text.Trim();
        }

        private bool _isInternalChanging = false; // 内部的な変更中かどうかを保持

        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // ズーム中（内部変更中）なら何もしない
            if (_isInternalChanging) return;

            // プログラムによる変更時は処理をスキップ
            if (_isChanging) return;

            _isChanging = true;

            // sender を RichTextBox のインスタンスとして取得
            var rtb = sender as RichTextBox;
            if (rtb == null)
            {
                _isChanging = false;
                return;
            }

            // BeginChange / EndChange はインスタンスメソッドなので rtb 経由で呼ぶ
            rtb.BeginChange();
            try
            {
                ApplyHeadingHighlighting(rtb);
            }
            finally
            {
                rtb.EndChange();
                _isChanging = false;
            }

            RewriteMarkdownBrowser();
        }

        // RichTextBox内の段落をループして、Markdownの見出しやリストなどの行を判定し、条件に合う行に対して背景色や文字色を変更する
        private void ApplyHeadingHighlighting(RichTextBox rtb)
        {
            if (rtb?.Document == null) return;

            if (_isSimpleEditMode) return;              // シンプル編集モードならスキップ

            // RichTextBox内の全ブロックをループ
            foreach (var block in rtb.Document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    // 段落のテキストを取得
                    TextRange range = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);
                    string text = range.Text.TrimStart(); // 行頭の空白を除去して判定

                    if (IsMarkdownLine(text))
                    {
                        paragraph.Background = Brushes.GhostWhite;
                        paragraph.Foreground = Brushes.SteelBlue;
                        paragraph.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        // 条件に合わない場合は標準の色に戻す
                        paragraph.Background = Brushes.White;
                        paragraph.Foreground = Brushes.Black;
                        paragraph.FontWeight = FontWeights.Normal;
                    }
                }
            }
        }

        // Ctrl + マウスホイールでフォントサイズを変更するイベントハンドラー
        private void RichMarkdownTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                _isInternalChanging = true; // ★フラグを立てる

                try
                {
                    double currentSize = RichMarkdownTextBox.FontSize;
                    double newSize;

                    // 1. フォントサイズの増減計算
                    if (e.Delta > 0) newSize = currentSize + 2;
                    else newSize = currentSize - 2;

                    // 制限範囲内なら適用
                    if (newSize >= 10 && newSize <= 60)
                    {
                        // 2. RichTextBox自体のFontSizeを更新
                        RichMarkdownTextBox.FontSize = newSize;

                        // 3. 適正な LineHeight を計算（例：フォントサイズの1.5倍）
                        double multiplier = 1.5;
                        double newLineHeight = newSize * multiplier;

                        // 4. 全ての段落に適用（重要）
                        foreach (var block in RichMarkdownTextBox.Document.Blocks)
                        {
                            if (block is Paragraph p)
                            {
                                p.LineHeight = newLineHeight;
                            }
                        }
                    }
                }
                finally
                {
                    _isInternalChanging = false; // ★必ず最後にfalseに戻す
                }

                e.Handled = true;
            }
        }

        private void RichMarkdownTextBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    string droppedFile = files[0];

                    string text = File.ReadAllText(droppedFile);

                    TextRange range = new TextRange(
                        RichMarkdownTextBox.Document.ContentStart,
                        RichMarkdownTextBox.Document.ContentEnd
                    );

                    range.Text = text;

                    _currentEditFileName = droppedFile;
                    _currentEditFolder = System.IO.Path.GetDirectoryName(droppedFile);

                    lblEditFile.Content = _currentEditFileName;
                    lblEditFile.Foreground = Brushes.Orange;
                }
                catch (Exception ex)
                {
                    // ファイルがロックされている、権限がない等のエラー対応
                    MessageBox.Show($"ファイルの読み込みに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                MouseCursorReset(sender, e);
            }
        }

        private void RichMarkdownTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            int acceptDrugFileCount = 1; // ドロップを受け入れるファイルの数（ここでは1つだけ）

            var drugFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

            string drugFileFullPath = drugFiles[0];

            // ファイルが1つだけで、拡張子が .md なら受け入れ
            if (drugFiles.Length == acceptDrugFileCount && Path.GetExtension(drugFileFullPath) == ".md")
            {
                e.Effects = DragDropEffects.Copy;
                Mouse.OverrideCursor = Cursors.Hand;
            }
            else
            {
                MouseCursorReset(sender, e);
            }

            // イベントを完了（標準の動作を抑制）
            e.Handled = true;
        }

        private void RichMarkdownTextBox_PreviewDragLeave(object sender, DragEventArgs e)
        {
            MouseCursorReset(sender, e);
        }

        private void MouseCursorReset(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            Mouse.OverrideCursor = null;
        }

        private bool IsMarkdownLine(string text)
        {
            // 行頭の空白をトリミングして判定
            string line = text.TrimStart();

            List<Regex> MarkdownPatterns = new List<Regex>
            {
                new Regex(@"^#{1,6}\s", RegexOptions.Compiled),         // 見出し: #〜######
                //new Regex(@"^```", RegexOptions.Compiled),              // コードブロック
                new Regex(@"^!\[.*?\]\(.*?\)", RegexOptions.Compiled)   // 画像構文
            };

            // LINQのAnyを使用して、1つでも一致(IsMatch)するものがあるか確認
            return MarkdownPatterns.Any(reg => reg.IsMatch(line)); ;
        }

        private void BtnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentEditFileName)) return;

            // RichTextBox 全文を TextRange として取得
            var textRange = new TextRange(RichMarkdownTextBox.Document.ContentStart, RichMarkdownTextBox.Document.ContentEnd);
            File.WriteAllText(_currentEditFileName, textRange.Text);
        }

        private void BtnFolder_Click(object sender, RoutedEventArgs e)
        {
            ShowSaveDialog();
        }

        private void chkSimpleEdit_Checked(object sender, RoutedEventArgs e)
        {
            _isSimpleEditMode = true;
            chkSimpleEdit.Foreground = Brushes.White;
        }

        private bool _isSimpleEditMode = true;
        private void chkSimpleEdit_Unchecked(object sender, RoutedEventArgs e)
        {
            _isSimpleEditMode = false;
            chkSimpleEdit.Foreground = Brushes.DarkGray;
        }

        public void ShowSaveDialog()
        {
            var dialog = new SaveFileDialog
            {
                Title = "名前を付けて保存",
                Filter = "マークダウンファイル (*.md)|*.md",
                FileName = "新しいファイル.md",
                InitialDirectory = "C:\\"
            };

            if (string.IsNullOrEmpty(_currentEditFileName) == false)
            {
                dialog.InitialDirectory = System.IO.Path.GetDirectoryName(_currentEditFileName);
                dialog.FileName = System.IO.Path.GetFileName(_currentEditFileName);
            }

            if (dialog.ShowDialog() == true)
            {
                // 選択されたファイルパス
                _currentEditFileName = dialog.FileName;
                _currentEditFolder = System.IO.Path.GetDirectoryName(dialog.FileName);
                var textRange = new TextRange(RichMarkdownTextBox.Document.ContentStart, RichMarkdownTextBox.Document.ContentEnd);
                File.WriteAllText(_currentEditFileName, textRange.Text);
            }
        }

        private void BtnClose_MouseEnter(object sender, MouseEventArgs e)
        {
            BtnClose.Background = Brushes.IndianRed;
            BtnClose.Foreground = Brushes.White;
        }

        private void BtnClose_MouseLeave(object sender, MouseEventArgs e)
        {
            BtnClose.Background = Brushes.Transparent;
            BtnClose.Foreground = Brushes.White;
        }
    }
}
