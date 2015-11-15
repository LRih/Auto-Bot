using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoBot
{
    public class CodeEditor : UserControl
    {
        //#==================================================================== EVENTS
        public event KeyPressEventHandler KeyPress;
        public event EventHandler TextChanged;

        //#==================================================================== CONSTANTS
        private readonly Color COL_LINE_NUMBERS = Color.FromArgb(85, 145, 175);

        //#==================================================================== VARIABLES
        private TextBoxEx _txtCode = new TextBoxEx();

        //#==================================================================== INITIALIZE
        public CodeEditor()
        {
            this.ClientSize = new Size(300, 300);
            _txtCode.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            _txtCode.BorderStyle = BorderStyle.None;
            _txtCode.Location = new Point(PanelWidth, 1);
            _txtCode.Size = new Size(ClientSize.Width - PanelWidth - 1, ClientSize.Height - 2);
            _txtCode.KeyPress += txtCode_KeyPress;
            _txtCode.TextChanged += txtCode_TextChanged;
            _txtCode.Redraw += txtCode_Redraw;
            this.DoubleBuffered = true;
            this.Controls.Add(_txtCode);
        }

        //#==================================================================== FUNCTIONS
        public int GetFirstCharIndexFromLine(int lineNumber)
        {
            return _txtCode.GetFirstCharIndexFromLine(lineNumber);
        }
        public int GetLineFromCharIndex(int index)
        {
            return _txtCode.GetLineFromCharIndex(index);
        }
        public void ScrollToCaret()
        {
            _txtCode.ScrollToCaret();
        }
        public void SelectAll()
        {
            _txtCode.SelectAll();
        }

        //#==================================================================== PROPERTIES
        private int PanelWidth
        {
            get { return TextRenderer.MeasureText("0000", this.Font).Width + 5; }
        }
        private int FirstLineNumber
        {
            get
            {
                int firstCharIndex =  _txtCode.GetCharIndexFromPosition(new Point(1, 1));
                return _txtCode.GetLineFromCharIndex(firstCharIndex) + 1;
            }
        }
        public void SelectLine(int line)
        {
            _txtCode.SelectionStart = _txtCode.GetFirstCharIndexFromLine(line);
            _txtCode.SelectionLength = _txtCode.Lines[line].Length;
        }

        public bool HideSelection
        {
            get { return _txtCode.HideSelection; }
            set { _txtCode.HideSelection = value; }
        }
        public bool Multiline
        {
            get { return _txtCode.Multiline; }
            set { _txtCode.Multiline = value; }
        }
        public string[] Lines
        {
            get { return _txtCode.Lines; }
            set { _txtCode.Lines = value; }
        }
        public bool ReadOnly
        {
            get { return _txtCode.ReadOnly; }
            set { _txtCode.ReadOnly = value; }
        }
        public ScrollBars ScrollBars
        {
            get { return _txtCode.ScrollBars; }
            set { _txtCode.ScrollBars = value; }
        }
        public int SelectionLength
        {
            get { return _txtCode.SelectionLength; }
            set { _txtCode.SelectionLength = value; }
        }
        public int SelectionStart
        {
            get { return _txtCode.SelectionStart; }
            set { _txtCode.SelectionStart = value; }
        }
        public string Text
        {
            get { return _txtCode.Text; }
            set { _txtCode.Text = value; }
        }
        public bool WordWrap
        {
            get { return _txtCode.WordWrap; }
            set { _txtCode.WordWrap = value; }
        }

        //#==================================================================== EVENTS
        protected override void OnPaint(PaintEventArgs e)
        {
            // draw border
            Rectangle rectBorder = new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            ControlPaint.DrawVisualStyleBorder(e.Graphics, rectBorder);
            // draw line numbers
            int fontHeight = TextRenderer.MeasureText("X", _txtCode.Font).Height;
            for (int line = FirstLineNumber; line <= _txtCode.Lines.Length; line++)
            {
                int lineY = 1 + (line - FirstLineNumber) * fontHeight;
                if (lineY + fontHeight > _txtCode.Height - 16) break;
                Rectangle rect = new Rectangle(_txtCode.Left, lineY, 1, 1);
                TextRenderer.DrawText(e.Graphics, line.ToString(), this.Font, rect, COL_LINE_NUMBERS, TextFormatFlags.Right | TextFormatFlags.NoClipping);
            }
            base.OnPaint(e);
        }
        private void txtCode_Redraw(object sender, EventArgs e)
        {
            this.Invalidate(false);
        }
        private void txtCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            this.Invalidate(false);
            if (KeyPress != null) KeyPress(sender, e);
        }
        private void txtCode_TextChanged(object sender, EventArgs e)
        {
            if (TextChanged != null) TextChanged(sender, e);
        }

        //#==================================================================== CLASSES
        private class TextBoxEx : TextBox
        {
            private const int WM_PAINT = 0x000F;
            public EventHandler Redraw;
            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                if (m.Msg == WM_PAINT)
                {
                    if (Redraw != null) Redraw(this, new EventArgs());
                }
            }
        }
    }
}
