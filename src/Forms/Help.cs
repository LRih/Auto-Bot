using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace AutoBot
{
    public class Help : Form
    {
        //#==================================================================== CONSTANTS
        private const int MARGIN = 12;

        //#==================================================================== VARIABLES
        private ListBox _lstHelp = new ListBox();
        private TextBox _txtHelp = new TextBox();

        //#==================================================================== INITIALIZE
        public Help()
        {
            this.ClientSize = new Size(580, 300);
            _lstHelp.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
            _lstHelp.IntegralHeight = false;
            _lstHelp.Items.AddRange(new object[] { "General Info", "Commands", "Type Command", "Commenting", "Functions", "Hotkeys" });
            _lstHelp.Location = new Point(MARGIN, MARGIN);
            _lstHelp.Size = new Size(120, ClientSize.Height - MARGIN * 2);
            _lstHelp.SelectedIndexChanged += lstHelp_SelectedIndexChanged;
            _txtHelp.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            _txtHelp.BackColor = SystemColors.Control;
            _txtHelp.BorderStyle = BorderStyle.None;
            _txtHelp.Font = new Font("Consolas", 10);
            _txtHelp.Location = new Point(_lstHelp.Right + 6, MARGIN);
            _txtHelp.Multiline = true;
            _txtHelp.ReadOnly = true;
            _txtHelp.ScrollBars = ScrollBars.Both;
            _txtHelp.Size = new Size(ClientSize.Width - _lstHelp.Right - 6 - MARGIN, ClientSize.Height - MARGIN * 2);
            _txtHelp.WordWrap = false;

            this.MinimizeBox = false;
            this.MinimumSize = new Size(300, 300);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = SizeGripStyle.Hide;
            this.Text = "Help";
            this.Controls.Add(_lstHelp);
            this.Controls.Add(_txtHelp);
        }

        //#==================================================================== EVENTS
        private void lstHelp_SelectedIndexChanged(object sender, EventArgs e)
        {
            _txtHelp.Clear();
            if (_lstHelp.SelectedItem == null) return;
            using (StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("AutoBot.Help.txt")))
            {
                bool startedReading = false;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line == "[" + _lstHelp.SelectedItem + "]") startedReading = true;
                    else if (startedReading)
                    {
                        if (line.StartsWith("[")) break;
                        else _txtHelp.Text += line + Environment.NewLine;
                    }
                }
            }
        }
    }
}
