using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FarsiLibrary.Win;
using FastColoredTextBoxNS;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace JSON_Editor
{
    public partial class MainWindow : Form
    {
        Color changedLineColor = Color.FromArgb(255, 230, 230, 255);

        public MainWindow()
        {
            InitializeComponent();

            //init menu images
            ComponentResourceManager resources = new ComponentResourceManager(typeof(MainWindow));
            copyToolStripMenuItem.Image = ((Image)(resources.GetObject("copyToolStripButton.Image")));
            cutToolStripMenuItem.Image = ((Image)(resources.GetObject("cutToolStripButton.Image")));
            pasteToolStripMenuItem.Image = ((Image)(resources.GetObject("pasteToolStripButton.Image")));
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.Image = ((Image)(resources.GetObject("newToolStripMenuItem.Image")));
            newToolStripMenuItem.ImageTransparentColor = Color.Magenta;
            newToolStripMenuItem.ShortcutKeys = ((Keys)((Keys.Control | Keys.N)));
            newToolStripMenuItem.Text = "&New";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Image = ((Image)(resources.GetObject("openToolStripMenuItem.Image")));
            openToolStripMenuItem.ImageTransparentColor = Color.Magenta;
            openToolStripMenuItem.ShortcutKeys = ((Keys)((Keys.Control | Keys.O)));
            openToolStripMenuItem.Text = "&Open";
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Image = ((Image)(resources.GetObject("saveToolStripMenuItem.Image")));
            saveToolStripMenuItem.ImageTransparentColor = Color.Magenta;
            saveToolStripMenuItem.ShortcutKeys = ((Keys)((Keys.Control | Keys.S)));
            saveToolStripMenuItem.Text = "&Save";

            tsbVerify.Click += TsbVerify_Click;
            
            EventHandler cutHndl = (s, e) => CurrentTB.Cut();
            cutToolStripMenuItem.Click += cutHndl;
            cutToolStripButton.Click += cutHndl;

            EventHandler copyHndl = (s, e) => CurrentTB.Copy();
            copyToolStripMenuItem.Click += copyHndl;
            copyToolStripButton.Click += copyHndl;

            EventHandler pasteHndl = (s, e) => CurrentTB.Paste();
            pasteToolStripMenuItem.Click += pasteHndl;
            pasteToolStripButton.Click += pasteHndl;

            selectAllToolStripMenuItem.Click += (s, e) => CurrentTB.Selection.SelectAll();

            EventHandler newFileHndl = (s, e) =>
            {
                CreateTab(null);
                lbStatusStrip.Text = "New empy file.";
            };
            newToolStripMenuItem.Click += newFileHndl;
            newToolStripButton.Click += newFileHndl;
            
            quitToolStripMenuItem.Click += (s, e) => Close();
            findToolStripMenuItem.Click += (s, e) => CurrentTB.ShowFindDialog();
            replaceToolStripMenuItem.Click += (s, e) => CurrentTB.ShowReplaceDialog();
        }

        private void TsbVerify_Click(object sender, EventArgs e)
        {
            if (null == CurrentTB)
            {
                MessageBox.Show(this, "File is not opened.", "File is not opened.",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string fileContent = CurrentTB.Text;

            if (string.IsNullOrEmpty(fileContent))
            {
                MessageBox.Show(this, "File is empty.",  "File is empty.",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var sb = new StringBuilder();
            sb.Append("The file is ");
            ValidationResult result = TextProcessor.isValidJson(fileContent);
            if (result.IsValid)
            {
                sb.Append("VALID ");
            }
            else
            {
                sb.Append("NOT valid ");
            }
            sb.Append("JSON file!");
            if (!result.IsValid)
            {
                sb.Append("\nThe reason can be somewhere near line ")
                    .Append(result.Row)
                      .Append(", character ").Append(result.At);
            }
            
            MessageBox.Show(this, sb.ToString(), "Validation result",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private Style sameWordsStyle 
            = new MarkerStyle(new SolidBrush(Color.FromArgb(50, Color.Gray)));

        private void CreateTab(string fileName)
        {
            try
            {
                var tb = new FastColoredTextBox();
                tb.Font = new Font("Consolas", 9.75f);
                tb.ContextMenuStrip = cmMain;
                tb.Dock = DockStyle.Fill;
                tb.BorderStyle = BorderStyle.Fixed3D;
                //tb.VirtualSpace = true;
                tb.LeftPadding = 17;

                tb.Language = Language.JSON;
                tb.SyntaxHighlighter = new JsonSyntaxHighlighter(tb);

                tb.AddStyle(sameWordsStyle);//same words style
                var tab = new FATabStripItem(fileName!=null?Path.GetFileName(fileName):"[new]", tb);
                tab.Tag = fileName;
                if (fileName != null)
                    tb.OpenFile(fileName);
                tb.Tag = new TbInfo();
                tsFiles.AddTab(tab);
                tsFiles.SelectedItem = tab;
                tb.Focus();
                tb.DelayedTextChangedInterval = 1000;
                tb.DelayedEventsInterval = 500;
                tb.TextChangedDelayed += new EventHandler<TextChangedEventArgs>(tb_TextChangedDelayed);
                tb.SelectionChangedDelayed += new EventHandler(tb_SelectionChangedDelayed);
                tb.KeyDown += new KeyEventHandler(tb_KeyDown);
                tb.ChangedLineColor = changedLineColor;
                tb.HighlightingRangeType = HighlightingRangeType.VisibleRange;
                //create autocomplete popup menu
                AutocompleteMenu popupMenu = new AutocompleteMenu(tb);
                popupMenu.Items.ImageList = ilAutocomplete;
                popupMenu.Opening += new EventHandler<CancelEventArgs>(popupMenu_Opening);
//                BuildAutocompleteMenu(popupMenu);
                (tb.Tag as TbInfo).popupMenu = popupMenu;
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.Retry)
                    CreateTab(fileName);
            }
        }

        void popupMenu_Opening(object sender, CancelEventArgs e)
        {
            //---block autocomplete menu for comments
            //get index of green style (used for comments)
            var iGreenStyle = CurrentTB.GetStyleIndex(CurrentTB.SyntaxHighlighter.GreenStyle);
            if (iGreenStyle >= 0)
                if (CurrentTB.Selection.Start.iChar > 0)
                {
                    //current char (before caret)
                    var c = CurrentTB[CurrentTB.Selection.Start.iLine][CurrentTB.Selection.Start.iChar - 1];
                    //green Style
                    var greenStyleIndex = Range.ToStyleIndex(iGreenStyle);
                    //if char contains green style then block popup menu
                    if ((c.style & greenStyleIndex) != 0)
                        e.Cancel = true;
                }
        }

        void tb_KeyDown(object sender, KeyEventArgs e)
        {      
            if (e.KeyData == (Keys.K | Keys.Control))
            {
                //forced show (MinFragmentLength will be ignored)
                (CurrentTB.Tag as TbInfo).popupMenu.Show(true);
                e.Handled = true;
            }
        }

        void tb_SelectionChangedDelayed(object sender, EventArgs e)
        {
            var tb = sender as FastColoredTextBox;
            //remember last visit time
            if (tb.Selection.IsEmpty && tb.Selection.Start.iLine < tb.LinesCount)
            {
                if (lastNavigatedDateTime != tb[tb.Selection.Start.iLine].LastVisit)
                {
                    tb[tb.Selection.Start.iLine].LastVisit = DateTime.Now;
                    lastNavigatedDateTime = tb[tb.Selection.Start.iLine].LastVisit;
                }
            }

            //highlight same words
            tb.VisibleRange.ClearStyle(sameWordsStyle);
            if (!tb.Selection.IsEmpty)
                return;//user selected diapason
            //get fragment around caret
            var fragment = tb.Selection.GetFragment(@"\w");
            string text = fragment.Text;
            if (text.Length == 0)
                return;
            //highlight same words
            Range[] ranges = tb.VisibleRange.GetRanges("\\b" + text + "\\b").ToArray();

            if (ranges.Length > 1)
                foreach (var r in ranges)
                    r.SetStyle(sameWordsStyle);
        }

        void tb_TextChangedDelayed(object sender, TextChangedEventArgs e)
        {
            FastColoredTextBox tb = (sender as FastColoredTextBox);
            string text = tb.Text;
            //recalculate words, characters, lines
            ThreadPool.QueueUserWorkItem(
                (o) =>
                {
                    TextStatistics result = TextProcessor.CountTextStatistics(text);
                    lbStatusStrip.Text = $"Lines: { result.Lines}, Words: { result.Words }, "
                        + $"Keys: {result.Keys}, Values: {result.Values}, "
                        + $"All symbols: { text.Length }";
                }
            );
        }

        private void tsFiles_TabStripItemClosing(TabStripItemClosingEventArgs e)
        {
            if ((e.Item.Controls[0] as FastColoredTextBox).IsChanged)
            {
                switch(MessageBox.Show("Do you want save " + e.Item.Title + " ?", 
                    "Save", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information))
                {
                    case System.Windows.Forms.DialogResult.Yes:
                        if (!Save(e.Item))
                            e.Cancel = true;
                        break;
                    case DialogResult.Cancel:
                         e.Cancel = true;
                        break;
                }
            }
        }

        private bool Save(FATabStripItem tab)
        {
            var tb = (tab.Controls[0] as FastColoredTextBox);
            if (tab.Tag == null)
            {
                if (sfdMain.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return false;
                tab.Title = Path.GetFileName(sfdMain.FileName);
                tab.Tag = sfdMain.FileName;
            }

            try
            {
                File.WriteAllText(tab.Tag as string, tb.Text);
                tb.IsChanged = false;
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                    return Save(tab);
                else
                    return false;
            }

            tb.Invalidate();

            return true;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tsFiles.SelectedItem != null)
                Save(tsFiles.SelectedItem);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tsFiles.SelectedItem != null)
            {
                string oldFile = tsFiles.SelectedItem.Tag as string;
                tsFiles.SelectedItem.Tag = null;
                if (!Save(tsFiles.SelectedItem))
                if(oldFile!=null)
                {
                    tsFiles.SelectedItem.Tag = oldFile;
                    tsFiles.SelectedItem.Title = Path.GetFileName(oldFile);
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ofdMain.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CreateTab(ofdMain.FileName);
            }

            lbStatusStrip.Text = "File is opened.";
        }

        FastColoredTextBox CurrentTB
        {
            get {
                if (tsFiles.SelectedItem == null)
                    return null;
                return (tsFiles.SelectedItem.Controls[0] as FastColoredTextBox);
            }

            set
            {
                tsFiles.SelectedItem = (value.Parent as FATabStripItem);
                value.Focus();
            }
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB.UndoEnabled)
                CurrentTB.Undo();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB.RedoEnabled)
                CurrentTB.Redo();
        }

        private void tmUpdateInterface_Tick(object sender, EventArgs e)
        {
            try
            {
                if(CurrentTB != null && tsFiles.Items.Count>0)
                {
                    var tb = CurrentTB;
                    undoStripButton.Enabled = undoToolStripMenuItem.Enabled = tb.UndoEnabled;
                    redoStripButton.Enabled = redoToolStripMenuItem.Enabled = tb.RedoEnabled;
                    saveToolStripButton.Enabled = saveToolStripMenuItem.Enabled = tb.IsChanged;
                    saveAsToolStripMenuItem.Enabled = true;
                    pasteToolStripButton.Enabled = pasteToolStripMenuItem.Enabled = true;
                    cutToolStripButton.Enabled = cutToolStripMenuItem.Enabled =
                    copyToolStripButton.Enabled = copyToolStripMenuItem.Enabled = !tb.Selection.IsEmpty;
                }
                else
                {
                    saveToolStripButton.Enabled = saveToolStripMenuItem.Enabled = false;
                    saveAsToolStripMenuItem.Enabled = false;
                    cutToolStripButton.Enabled = cutToolStripMenuItem.Enabled =
                    copyToolStripButton.Enabled = copyToolStripMenuItem.Enabled = false;
                    pasteToolStripButton.Enabled = pasteToolStripMenuItem.Enabled = false;
                    undoStripButton.Enabled = undoToolStripMenuItem.Enabled = false;
                    redoStripButton.Enabled = redoToolStripMenuItem.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        bool tbFindChanged = false;

        private void tbFind_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' && CurrentTB != null)
            {
                Range r = tbFindChanged?CurrentTB.Range.Clone():CurrentTB.Selection.Clone();
                tbFindChanged = false;
                r.End = new Place(CurrentTB[CurrentTB.LinesCount - 1].Count, CurrentTB.LinesCount - 1);
                var pattern = Regex.Escape(tbFind.Text);
                foreach (var found in r.GetRanges(pattern))
                {
                    found.Inverse();
                    CurrentTB.Selection = found;
                    CurrentTB.DoSelectionVisible();
                    return;
                }
                MessageBox.Show("Not found.");
            }
            else
                tbFindChanged = true;
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            List<FATabStripItem> list = new List<FATabStripItem>();
            foreach (FATabStripItem tab in  tsFiles.Items)
                list.Add(tab);
            foreach (var tab in list)
            {
                TabStripItemClosingEventArgs args = new TabStripItemClosingEventArgs(tab);
                tsFiles_TabStripItemClosing(args);
                if (args.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                tsFiles.RemoveTab(tab);
            }
        }

        private void tsFiles_TabStripItemSelectionChanged(TabStripItemChangedEventArgs e)
        {
            if (CurrentTB != null)
            {
                CurrentTB.Focus();
                string text = CurrentTB.Text;
            }
        }

        DateTime lastNavigatedDateTime = DateTime.Now;

        /// <summary>
        /// This item appears when any part of snippet text is typed
        /// </summary>
        class DeclarationSnippet : SnippetAutocompleteItem
        {
            public DeclarationSnippet(string snippet)
                : base(snippet)
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                var pattern = Regex.Escape(fragmentText);
                if (Regex.IsMatch(Text, "\\b" + pattern, RegexOptions.IgnoreCase))
                    return CompareResult.Visible;
                return CompareResult.Hidden;
            }
        }

        /// <summary>
        /// Divides numbers and words: "123AND456" -> "123 AND 456"
        /// Or "i=2" -> "i = 2"
        /// </summary>
        class InsertSpaceSnippet : AutocompleteItem
        {
            string pattern;

            public InsertSpaceSnippet(string pattern)
                : base("")
            {
                this.pattern = pattern;
            }

            public InsertSpaceSnippet()
                : this(@"^(\d+)([a-zA-Z_]+)(\d*)$")
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                if (Regex.IsMatch(fragmentText, pattern))
                {
                    Text = InsertSpaces(fragmentText);
                    if (Text != fragmentText)
                        return CompareResult.Visible;
                }
                return CompareResult.Hidden;
            }

            public string InsertSpaces(string fragment)
            {
                var m = Regex.Match(fragment, pattern);
                if (m == null)
                    return fragment;
                if (m.Groups[1].Value == "" && m.Groups[3].Value == "")
                    return fragment;
                return (m.Groups[1].Value + " " + m.Groups[2].Value + " " + m.Groups[3].Value).Trim();
            }

            public override string ToolTipTitle
            {
                get
                {
                    return Text;
                }
            }
        }

        /// <summary>
        /// Inerts line break after '}'
        /// </summary>
        class InsertEnterSnippet : AutocompleteItem
        {
            Place enterPlace = Place.Empty;

            public InsertEnterSnippet()
                : base("[Line break]")
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                var r = Parent.Fragment.Clone();
                while (r.Start.iChar > 0)
                {
                    if (r.CharBeforeStart == '}')
                    {
                        enterPlace = r.Start;
                        return CompareResult.Visible;
                    }

                    r.GoLeftThroughFolded();
                }

                return CompareResult.Hidden;
            }

            public override string GetTextForReplace()
            {
                //extend range
                Range r = Parent.Fragment;
                Place end = r.End;
                r.Start = enterPlace;
                r.End = r.End;
                //insert line break
                return Environment.NewLine + r.Text;
            }

            public override string ToolTipTitle
            {
                get
                {
                    return "Insert line break after '}'";
                }
            }
        }

        private void autoIndentSelectedTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.DoAutoIndent();
        }

        private void commentSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.InsertLinePrefix("//");
        }

        private void uncommentSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.RemoveLinePrefix("//");
        }

        private void cloneLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //expand selection
            CurrentTB.Selection.Expand();
            //get text of selected lines
            string text = Environment.NewLine + CurrentTB.Selection.Text;
            //move caret to end of selected lines
            CurrentTB.Selection.Start = CurrentTB.Selection.End;
            //insert text
            CurrentTB.InsertText(text);
        }

        private void cloneLinesAndCommentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //start autoUndo block
            CurrentTB.BeginAutoUndo();
            //expand selection
            CurrentTB.Selection.Expand();
            //get text of selected lines
            string text = Environment.NewLine + CurrentTB.Selection.Text;
            //comment lines
            CurrentTB.InsertLinePrefix("//");
            //move caret to end of selected lines
            CurrentTB.Selection.Start = CurrentTB.Selection.End;
            //insert text
            CurrentTB.InsertText(text);
            //end of autoUndo block
            CurrentTB.EndAutoUndo();
        }

        private void Zoom_click(object sender, EventArgs e)
        {
            if (CurrentTB != null)
                CurrentTB.Zoom = int.Parse((sender as ToolStripItem).Tag.ToString());
        }
    }

    public class TbInfo
    {
        public AutocompleteMenu popupMenu;
    }
}
