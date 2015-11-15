using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace AutoBot
{
    public class AutoBot : Form
    {
        //#==================================================================== CONSTANTS
        private const Keys START = Keys.F11;
        private readonly Color COL_NOT_COMPILED = Color.FromArgb(255, 128, 128);
        private readonly Color COL_COMPILED = Color.FromArgb(128, 255, 128);
        private const int MARGIN = 10;

        //#==================================================================== CONTROLS
        private IContainer _components = new Container();
        private CodeEditor _txtCode = new CodeEditor();
        private Button _btnLoad = new Button();
        private Button _btnCompile = new Button();
        private NumericUpDown _numWait = new NumericUpDown();
        private CheckBox _chkTopmost = new CheckBox();
        private CheckBox _chkLoop = new CheckBox();
        private Timer _timer;

        //#==================================================================== VARIABLES
        private System.Threading.Mutex _singleInstanceMutex; // used to ensure only one instance is running at any time
        private Dictionary<Keys, RCommandType> _hotkeys = new Dictionary<Keys, RCommandType>(); // holds hotkeys
        private bool _isKeyDown = false; // check if pressing hotkey flag
        private bool _isScriptRunning = false; // check if script running flag
        private bool _isCommandRunning = false; // check if command running flag
        private RScript _script = new RScript();

        //#==================================================================== INITIALIZE
        public AutoBot()
        {
            this.ClientSize = new Size(400, 300);
            _txtCode.AllowDrop = true;
            _txtCode.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            _txtCode.Font = new Font("Consolas", 9.75f);
            _txtCode.HideSelection = false;
            _txtCode.Multiline = true;
            _txtCode.ScrollBars = ScrollBars.Both;
            _txtCode.Text = "move_star\r\n{\r\n  move > 900,712\r\n  move > 738,176\r\n  move > 546,765\r\n  move > 1041,389\r\n  move > 375,379\r\n}\r\n\r\nmove_star>\r\nmove > 0,0";
            _txtCode.WordWrap = false;
            _txtCode.DragDrop += txtCode_DragDrop;
            _txtCode.DragEnter += txtCode_DragEnter;
            _txtCode.KeyPress += txtCode_KeyPress;
            _txtCode.TextChanged += txtCode_TextChanged;
            _btnLoad.Location = new Point(MARGIN, MARGIN);
            _btnLoad.Text = "Load Macro";
            _btnLoad.Click += btnLoad_Click;
            Button btnSave = new Button();
            btnSave.Location = new Point(MARGIN, _btnLoad.Bottom + 6);
            btnSave.Text = "Save Macro";
            btnSave.Click += btnSave_Click;
            _btnCompile.BackColor = COL_NOT_COMPILED;
            _btnCompile.Location = new Point(MARGIN, btnSave.Bottom + 6);
            _btnCompile.Height *= 2;
            _btnCompile.Text = "Compile";
            _btnCompile.Click += btnCompile_Click;
            _numWait.Increment = 300;
            _numWait.Location = new Point(MARGIN, _btnCompile.Bottom + 6);
            _numWait.Maximum = 100000;
            _numWait.Width = _btnCompile.Width;
            _numWait.TabStop = false;
            _numWait.Value = 300;
            _chkTopmost.AutoSize = true;
            _chkTopmost.Location = new Point(MARGIN, _numWait.Bottom + 6);
            _chkTopmost.Text = "Topmost";
            _chkTopmost.CheckedChanged += chkTopmost_CheckedChanged;
            _chkLoop.AutoSize = true;
            _chkLoop.Checked = true;
            _chkLoop.Location = new Point(MARGIN, _chkTopmost.Bottom);
            _chkLoop.Text = "Loop";
            Button btnHelp = new Button();
            btnHelp.Location = new Point(MARGIN, _chkLoop.Bottom);
            btnHelp.Text = "Help";
            btnHelp.Click += btnHelp_Click;
            _timer = new Timer(_components);
            _timer.Enabled = true;
            _timer.Interval = 10;
            _timer.Tick += timer_Tick;
            _txtCode.Location = new Point(_btnLoad.Right + 6, MARGIN);
            _txtCode.Size = new Size(ClientSize.Width - _btnLoad.Right - 6 - MARGIN, ClientSize.Height - MARGIN * 2);
            this.Icon = new Icon(Assembly.GetCallingAssembly().GetManifestResourceStream("AutoBot.Icon.ico"));
            this.MinimumSize = new Size(300, 300);
            this.Controls.Add(_txtCode);
            this.Controls.Add(_btnLoad);
            this.Controls.Add(btnSave);
            this.Controls.Add(_btnCompile);
            this.Controls.Add(_numWait);
            this.Controls.Add(_chkTopmost);
            this.Controls.Add(_chkLoop);
            this.Controls.Add(btnHelp);
            this.DoubleBuffered = true;
            CheckMultipleInstances();
            InitializeHotkeys();
            InitializeOpenWithFile();
            StopMacro();
        }
        private void CheckMultipleInstances()
        {
            // initialize mutex
            try
            {
                // check if instance is already open
                _singleInstanceMutex = System.Threading.Mutex.OpenExisting("InstanceAutoBot");
                MessageBox.Show("Auto Bot is already running.", "Error");
                Process.GetCurrentProcess().Kill();
            }
            catch { _singleInstanceMutex = new System.Threading.Mutex(false, "InstanceAutoBot"); }
        }
        private void InitializeHotkeys()
        {
            _hotkeys.Add(Keys.F2, RCommandType.WaitUntil);
            _hotkeys.Add(Keys.F3, RCommandType.WaitWhile);
            _hotkeys.Add(Keys.F4, RCommandType.WaitBright);
            _hotkeys.Add(Keys.F5, RCommandType.WaitDark);
            _hotkeys.Add(Keys.F6, RCommandType.LeftDown);
            _hotkeys.Add(Keys.F7, RCommandType.LeftUp);
            _hotkeys.Add(Keys.F8, RCommandType.Left);
            _hotkeys.Add(Keys.F9, RCommandType.Right);
            _hotkeys.Add(Keys.F10, RCommandType.Wait);
        }
        private void InitializeOpenWithFile()
        {
            foreach (string arg in Environment.GetCommandLineArgs())
                if (arg.EndsWith(".txt")) _txtCode.Text = File.ReadAllText(arg);
        }

        //#==================================================================== TERMINATE
        protected override void Dispose(bool disposing)
        {
            _singleInstanceMutex.Dispose(); // dispose mutex
            if (disposing && _components != null) _components.Dispose();
            base.Dispose(disposing);
        }

        //#==================================================================== FUNCTIONS
        private void AddCommand(RCommandType command)
        {
            // determine command insertion point
            int caretPt = _txtCode.SelectionStart + _txtCode.SelectionLength;
            int insertionPt;
            if (_txtCode.Text.IndexOf(Environment.NewLine, caretPt) == -1) insertionPt = _txtCode.Text.Length;
            else insertionPt = _txtCode.Text.IndexOf(Environment.NewLine, caretPt);
            // insert command into text box
            string insertTxt = string.Empty;
            if (_txtCode.Lines.Length != 0 && _txtCode.Lines[_txtCode.GetLineFromCharIndex(insertionPt)].Length != 0) insertTxt = "\r\n";
            switch (command)
            {
                case RCommandType.Left:
                    insertTxt += string.Format("left > {0},{1}", MousePosition.X, MousePosition.Y); break;
                case RCommandType.LeftDown:
                    insertTxt += string.Format("ldown > {0},{1}", MousePosition.X, MousePosition.Y); break;
                case RCommandType.LeftUp:
                    insertTxt += "lup >"; break;
                case RCommandType.Right:
                    insertTxt += string.Format("right > {0},{1}", MousePosition.X, MousePosition.Y); break;
                case RCommandType.Wait:
                    insertTxt += insertTxt = string.Format("wait > {0}", _numWait.Value); break;
                case RCommandType.WaitUntil:
                    insertTxt += string.Format("waituntil > {0},{1},{2}", MousePosition.X, MousePosition.Y, GetWin32CursorColor()); break;
                case RCommandType.WaitWhile:
                    insertTxt += string.Format("waitwhile > {0},{1},{2}", MousePosition.X, MousePosition.Y, GetWin32CursorColor()); break;
                case RCommandType.WaitBright:
                    insertTxt += string.Format("waitbright > {0},{1},{2}", MousePosition.X, MousePosition.Y, GetCursorBrightness()); break;
                case RCommandType.WaitDark:
                    insertTxt += string.Format("waitdark > {0},{1},{2}", MousePosition.X, MousePosition.Y, GetCursorBrightness()); break;
            }
            _txtCode.Text = _txtCode.Text.Insert(insertionPt, insertTxt);
            _txtCode.SelectionStart = insertionPt + insertTxt.Length;
            _txtCode.SelectionLength = 0;
            _txtCode.ScrollToCaret();
        }
        private int GetWin32CursorColor()
        {
            return ColorTranslator.ToWin32(Macro.GetPixelEx(MousePosition));
        }
        private int GetCursorBrightness()
        {
            return (int)Math.Round(Macro.GetPixelEx(MousePosition).GetBrightness() * 100);
        }

        private void StartMacro()
        {
            _script.Reset();
            _txtCode.ReadOnly = true;
            this.Text = string.Format("Auto Bot v{0} - Running", Assembly.GetCallingAssembly().GetName().Version);
            _isScriptRunning = true;
        }
        private void StopMacro()
        {
            _txtCode.ReadOnly = false;
            this.Text = string.Format("Auto Bot v{0} - Idle", Assembly.GetCallingAssembly().GetName().Version);
            _isScriptRunning = false;
        }

        //#==================================================================== EVENTS
        private void timer_Tick(object sender, EventArgs e)
        {
            this.Invalidate(new Rectangle(MARGIN, ClientSize.Height - this.Font.Height * 3 - 12 - MARGIN, _txtCode.Left, this.Font.Height * 3 + 12), false);
            if (_isKeyDown) return;
            ToggleOnOff();

            if (_isCommandRunning) return;
            RunNextCommand();

            if (_isScriptRunning) return;
            if (_isKeyDown) return;
            UpdateHotkeys();
        }
        private void ToggleOnOff()
        {
            if (Hotkey.IsKeyDown(START) && !_isScriptRunning)
            {
                _isKeyDown = true;
                if (_btnCompile.Enabled) MessageBox.Show("Please compile before running.");
                else if (_script.IsEmpty) MessageBox.Show("Compiled macro has no commands.");
                else StartMacro();
                Hotkey.WaitUntilKeyUp(START);
            }
            else if (Hotkey.IsKeyDown(START) && _isScriptRunning)
            {
                _isKeyDown = true;
                StopMacro();
                Hotkey.WaitUntilKeyUp(START);
            }
            _isKeyDown = false;
        }
        private void RunNextCommand()
        {
            if (_isScriptRunning)
            {
                _isCommandRunning = true;
                _txtCode.SelectLine(_script.CurrentLine - 1); // highlight current command
                _script.RunNextCommand();
                if (!_chkLoop.Checked && _script.IsFinished) StopMacro(); 
                _isCommandRunning = false;
            }
        }
        private void UpdateHotkeys()
        {
            foreach (Keys key in _hotkeys.Keys)
            {
                if (Hotkey.IsKeyDown(key))
                {
                    _isKeyDown = true;
                    AddCommand(_hotkeys[key]);
                    Hotkey.WaitUntilKeyUp(key);
                    _isKeyDown = false;
                    return;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // update coords
            e.Graphics.DrawString(MousePosition.X + ", " + MousePosition.Y, this.Font, Brushes.Black, MARGIN, ClientSize.Height - this.Font.Height - MARGIN);
            // update color (both GetPixel & GetPixelEx are slow so disabled)
            //Color pixel = Macro.GetPixelEx(MousePosition);
            //e.Graphics.DrawString(pixel.R + ", " + pixel.G + ", " + pixel.B, this.Font, Brushes.Black, MARGIN, ClientSize.Height - this.Font.Height * 2 - 6 - MARGIN);
            //e.Graphics.FillRectangle(new SolidBrush(pixel), MARGIN, ClientSize.Height - this.Font.Height * 3 - 12 - MARGIN, 13, 13);
            base.OnPaint(e);
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.Invalidate(false);
            _txtCode.Invalidate();
            //this.Invalidate(new Rectangle(MARGIN, ClientSize.Height - this.Font.Height * 3 - 12 - MARGIN, _btnLoad.Width, this.Font.Height * 3 + 12), false);
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = Application.StartupPath;
            dialog.Filter = "Text File|*.txt";
            if (dialog.ShowDialog(this) == DialogResult.OK) _txtCode.Text = File.ReadAllText(dialog.FileName);
            dialog.Dispose();
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.InitialDirectory = Application.StartupPath;
            dialog.Filter = "Text File|*.txt";
            if (dialog.ShowDialog(this) == DialogResult.OK) File.WriteAllText(dialog.FileName, _txtCode.Text);
            dialog.Dispose();
        }
        private void btnCompile_Click(object sender, EventArgs e)
        {
            int compileResult = _script.Compile(_txtCode.Text);
            // set UI to show compiled
            if (compileResult == -1)
            {
                _btnCompile.BackColor = COL_COMPILED;
                _btnCompile.Text = "Compiled";
                _btnCompile.Enabled = false;
            }
            else
            {
                MessageBox.Show(string.Format("Compile error at line {0}", compileResult));
                _txtCode.SelectLine(compileResult - 1);
            }
        }
        private void chkTopmost_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = _chkTopmost.Checked;
        }
        private void btnHelp_Click(object sender, EventArgs e)
        {
            Help dialog = new Help();
            dialog.ShowDialog(this);
            dialog.Dispose();
        }

        private void txtCode_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string path = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            if (path.EndsWith(".txt")) e.Effect = DragDropEffects.Copy;
        }
        private void txtCode_DragDrop(object sender, DragEventArgs e)
        {
            string path = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            _txtCode.Text = File.ReadAllText(path);
        }
        private void txtCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\x1')
            {
                _txtCode.SelectAll();
                e.Handled = true;
            }
        }
        private void txtCode_TextChanged(object sender, EventArgs e)
        {
            _btnCompile.BackColor = COL_NOT_COMPILED;
            _btnCompile.Text = "Compile";
            _btnCompile.Enabled = true;
        }
    }
}
