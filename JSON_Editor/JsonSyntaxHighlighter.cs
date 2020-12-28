using System.Drawing;
using FastColoredTextBoxNS;
using System.Text.RegularExpressions;

namespace JSON_Editor
{
    internal class JsonSyntaxHighlighter : SyntaxHighlighter
    {

        public JsonSyntaxHighlighter(FastColoredTextBox currentTb) : base(currentTb)
        {
            var blueStyle = new TextStyle(Brushes.Blue, null, FontStyle.Regular);
            StringStyle = blueStyle;

            var greenStyle = new TextStyle(Brushes.Green, null, FontStyle.Italic);
            NumberStyle = greenStyle;
            
            var redBoldStyle = new TextStyle(Brushes.Red, null, FontStyle.Bold);
            KeywordStyle = redBoldStyle;

        }

        private void InitJsonRegex()
        {
            JSONStringRegex = new Regex(@"""([^\\""]|\\"")*""", RegexCompiledOption);
            JSONNumberRegex = new Regex(@"\b(\d+[\.]?\d*|true|false|null)\b", RegexCompiledOption);
            JSONKeywordRegex = new Regex(@"(?<range>""([^\\""]|\\"")*"")\s*:", RegexCompiledOption);
        }

        public override void JSONSyntaxHighlight(Range range)
        {
            range.tb.LeftBracket = '[';
            range.tb.RightBracket = ']';
            range.tb.LeftBracket2 = '{';
            range.tb.RightBracket2 = '}';
            range.tb.BracketsHighlightStrategy = BracketsHighlightStrategy.Strategy2;

            range.tb.AutoIndentCharsPatterns
                = @"
^\s*[\w\.]+(\s\w+)?\s*(?<range>=)\s*(?<range>[^;]+);
";

            //clear style of changed range
            range.ClearStyle(StringStyle, NumberStyle, KeywordStyle);
            //
            if (JSONStringRegex == null)
                InitJsonRegex();
            //keyword highlighting
            range.SetStyle(KeywordStyle, JSONKeywordRegex);
            //string highlighting
            range.SetStyle(StringStyle, JSONStringRegex);
            //number highlighting
            range.SetStyle(NumberStyle, JSONNumberRegex);
            //clear folding markers
            range.ClearFoldingMarkers();
            //set folding markers
            range.SetFoldingMarkers("{", "}"); //allow to collapse brackets block
            range.SetFoldingMarkers(@"\[", @"\]"); //allow to collapse comment block
        }


    }
}
