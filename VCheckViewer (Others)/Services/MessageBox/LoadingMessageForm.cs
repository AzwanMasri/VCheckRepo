using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCheckViewer_Others.Services.MessageBox
{
    public class LoadingMessageForm : Form
    {
        private Label label;

        public LoadingMessageForm(string message)
        {
            FormBorderStyle = FormBorderStyle.None; // removes title bar + X
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(300, 120);
            TopMost = true;

            label = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12)
            };

            Controls.Add(label);
        }

        //public void Show()
        //{
        //    using (var customMessageBox = new LoadingMessageForm("Processing...."))
        //    {
        //        customMessageBox.ShowDialog();
        //    }
        //}
    }
}
