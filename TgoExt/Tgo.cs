using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Input;
using System.Diagnostics;

namespace TgoExt
{
    class Tgo
    {
        private static Form overlay;
        private static Thread listener;

        /// <summary>
        /// External Initialization. Runs when Terraria begins. Use this to initialize any variables.
        /// Entry Point: Terraria.Main
        /// </summary>
        public static void ExtInit()
        {
            overlay = GenerateOverlay();
        }

        /// <summary>
        /// Listens for the Tilde key for now.
        /// </summary>
        private static void InputListener()
        {
            //brute force clean up crew
            overlay.FormClosed += (object o, FormClosedEventArgs e) =>
            {
                Process t = Process.GetCurrentProcess();
                t.Close();
                Process[] p = Process.GetProcessesByName("TGO.exe");
                foreach (Process x in p)
                    x.Close();
                p = Process.GetProcessesByName("Terraria.exe");
                foreach (Process x in p)
                    x.Close();
            };
            try
            {
                while (true)
                {
                    Thread.Sleep(1); //This is here so the thread doesn't immediately neck itself because no input is happening.
                    if (Keyboard.IsKeyDown(Key.OemTilde))
                    {
                        ToggleFormOverlay(overlay);
                        Thread.Sleep(500); //0.5 second delay so it can't be spammed.
                    }
                    else continue;
                }
            }
            catch (Exception e)
            {
                return;
            }
        }

        /// <summary>
        /// Generates a C# Form to act as the overlay.
        /// </summary>
        /// <returns></returns>
        public static Form GenerateOverlay()
        {
            Form f = new Form();
            f.Show();
            f.BringToFront();
            f.TopMost = true;
            f.Size = new Size(SystemInformation.VirtualScreen.Width - (int)(SystemInformation.VirtualScreen.Width * 0.9),
                SystemInformation.VirtualScreen.Height);
            f.Location = new Point(0, 0); //replace with terraria coordinates
            f.Opacity = 0.7; //replace with constant
            f.Disposed += Dispose; //clean up system hooks

            //Hide Form?
            //f.TransparencyKey = Color.Bisque;
            f.BackColor = Color.SlateGray; //Color.Bisque;
            f.FormBorderStyle = FormBorderStyle.None;
            //Cursor.Hide();

            //Buttons
            Button bT1 = MakeButton(new Point(0, 0), "T1");

            //Control Additions
            f.Controls.Add(bT1);

            //ToggleFormOverlay(f); //initially hide until hotkey combo is used.

            listener = new Thread(new ThreadStart(InputListener));
            listener.SetApartmentState(ApartmentState.STA);
            listener.Start();

            return f;
        }

        private static void Dispose(object sender, EventArgs e)
        {
            listener.Abort();
        }

        /// <summary>
        /// Creates a button. Might want to add Rect parameter for size.
        /// </summary>
        /// <param name="loc"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private static Button MakeButton(Point loc, string text)
        {
            Button b = new Button();
            b.BackColor = Color.White;
            b.ForeColor = Color.Black;
            b.Font = new Font(b.Font.FontFamily, 16, FontStyle.Bold);
            b.Height = 50;
            b.Width = 50;
            b.Location = loc;
            b.Text = text;
            b.Enabled = true;
            b.Visible = true;
            return b;
        }

        private static void ToggleFormOverlay(Form f)
        {
            foreach (Control c in f.Controls)
                c.Visible = !c.Visible;
            f.Visible = !f.Visible;
        }
    }
}
