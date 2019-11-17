using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Input;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace TgoExt
{
    /// <summary>
    /// TODO: Disable all controls on player logout.
    /// </summary>
    class Tgo
    {
        private static Form overlay;
        private static Thread listener;
        private static TcpClient server;
        private static string tplrName;
        private static Thread conThread;
        private static Thread plThread;

        //IPE Info from NodeCraft
        private static int PORT = 10337;
        private static IPAddress IP = IPAddress.Parse("173.236.15.24");

        private static List<Control> extControls;
        private static ListBox targets;
        private static TextBox tbReason;

        /// <summary>
        /// External Initialization. Runs when Terraria begins. Use this to initialize any variables.
        /// Entry Point: Terraria.Main
        /// </summary>
        public static void ExtInit()
        {
            try
            {
                extControls = new List<Control>();
                overlay = GenerateOverlay();

                conThread = new Thread(new ThreadStart(Connect));
                conThread.Start();

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
                    conThread.Abort();
                    plThread.Abort();
                };
            }
            catch (Exception e)
            {
                MessageBox.Show("TgoExt.ExtInit Error: " + e.Message);
            }
        }

        /// <summary>
        /// Listens for the Tilde key for now.
        /// </summary>
        private static void InputListener()
        {
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
            //f.Size = new Size(SystemInformation.VirtualScreen.Width - (int)(SystemInformation.VirtualScreen.Width * 0.9),
            //    SystemInformation.VirtualScreen.Height);
            f.Size = new Size(300, 500); //300, 500 seems to be a good size for the window.
            f.Location = new Point(0, 0); //replace with terraria coordinates
            f.Opacity = 0.7; //replace with constant
            f.Disposed += Dispose; //clean up system hooks
            f.BackColor = Color.SlateGray; //Color.Bisque;
            f.FormBorderStyle = FormBorderStyle.None;

            TabControl tc = new TabControl();
            tc.Width = f.Width;
            tc.Height = f.Height;

            #region TgoMod
            TabPage tgoModPage = new TabPage();
            tgoModPage.Text = "Tgo Mod";
            tgoModPage.Height = f.Height;
            tgoModPage.Width = f.Width;

            //Player targets
            targets = new ListBox();
            targets.Location = new Point(0, 0);
            targets.Width = 100;
            targets.Height = 100;
            targets.Enabled = false;
            extControls.Add(targets);
            tgoModPage.Controls.Add(targets);

            //kick/mute/ban reason textbox
            tbReason = new TextBox();
            tbReason.Location = new Point(0, 110);
            tbReason.Size = new Size(100, 30);
            tbReason.Font = new Font(tbReason.Font.FontFamily, 8, FontStyle.Bold);
            tbReason.Text = "Reason";
            tbReason.Click += (object sender, EventArgs e) => { tbReason.Text = ""; };
            tgoModPage.Controls.Add(tbReason);

            //Buttons
            //Button bT1 = MakeButton(new Point(110, 0), "T1", SendMessage);
            //tgoModPage.Controls.Add(bT1);
            Button mute = MakeButton(new Point(110, 0), "Mute", SendMessage);
            Button kick = MakeButton(new Point(110, 60), "Kick", SendMessage);
            Button ban = MakeButton(new Point(110, 120), "Ban", SendMessage);
            tgoModPage.Controls.AddRange(new Control[] { mute, kick, ban });

            tc.TabPages.Add(tgoModPage);
            #endregion
            #region TgoWEdit
            TabPage tgoEditPage = new TabPage();
            tgoEditPage.Text = "Tgo Edit";
            tgoEditPage.Height = f.Height;
            tgoEditPage.Width = f.Width;

            //Buttons
            Button point1 = MakeButton(new Point(0, 0), "Point1", SendMessage);
            Button point2 = MakeButton(new Point(0, 60), "Point2", SendMessage);
            Button cut = MakeButton(new Point(0, 120), "Cut", SendMessage);
            Button paste = MakeButton(new Point(110, 0), "Paste", SendMessage);
            Button undo = MakeButton(new Point(110, 60), "Undo", SendMessage);
            tgoEditPage.Controls.AddRange(new Control[] { point1, point2, cut, paste, undo });

            tc.TabPages.Add(tgoEditPage);
            #endregion
            f.Controls.Add(tc);

            //Buttons
            //Button bT1 = MakeButton(new Point(0, 0), "T1", SendMessage);
            

            //Control Additions
            //f.Controls.Add(bT1);

            //ToggleFormOverlay(f); //initially hide until hotkey combo is used.

            listener = new Thread(new ThreadStart(InputListener));
            listener.SetApartmentState(ApartmentState.STA);
            listener.Start();

            return f;
        }

        private static void Dispose(object sender, EventArgs e)
        {
            listener.Abort();
            conThread.Abort();
        }

        /// <summary>
        /// Creates a button. Might want to add Rect parameter for size.
        /// </summary>
        /// <param name="loc"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private static Button MakeButton(Point loc, string text, EventHandler OnClick)
        {
            Button b = new Button();
            b.BackColor = Color.White;
            b.ForeColor = Color.Black;
            b.Font = new Font(b.Font.FontFamily, 16, FontStyle.Bold);
            b.Height = 50;
            b.Width = 100;
            b.Location = loc;
            b.Text = text;
            b.Enabled = false; //disable all controls for now.
            b.Visible = true;
            b.Click += OnClick;
            extControls.Add(b);
            return b;
        }

        private static void SendMessage(object sender, EventArgs e)
        {
            StreamWriter sw = new StreamWriter(server.GetStream());
            Button bt = (Button)sender;
            sw.WriteLine(bt.Text + "," + targets.SelectedItem.ToString() + "," + tbReason.Text);
            sw.Flush();
        }

        private static void ToggleFormOverlay(Form f)
        {
            foreach (Control c in f.Controls)
                c.Visible = !c.Visible;
            f.Visible = !f.Visible;
        }

        private static void Connect()
        {
            try
            {
                //server = new TcpClient(IPE); //fails for some reason. connect outside the constructor from now on.
                server = new TcpClient();
                server.Connect(IP, PORT);
                StreamReader sr = new StreamReader(server.GetStream());
                tplrName = sr.ReadLine(); //should wait for the tsplr name to be sent...
                if (tplrName != null)
                {
                    //activate controls
                    foreach (Control c in extControls)
                        c.Enabled = true;
                    //begin listening for player list.
                    plThread = new Thread(new ThreadStart(ListenPlayerListAsync));
                    plThread.Start();
                }
                else MessageBox.Show("Null TPlayer Name Exception");
            }
            catch (Exception e)
            {
                MessageBox.Show("Connection error: " + e.Message);
            }
        }

        /// <summary>
        /// TODO: Have TgoRequests send info in below format whenever a player logs in or logs out.
        /// Update a player control list.
        /// </summary>
        private static async void ListenPlayerListAsync()
        {
            try
            {
                StreamReader sr = new StreamReader(server.GetStream());
                while (true)
                {
                    //PL,name1,name2...
                    string list = await sr.ReadLineAsync();
                    string[] tkns = list.Split(',');
                    if (tkns[0] == "PL")
                    {
                        //for (int i = 0; i < targets.Items.Count; i++)
                        //    targets.Items.RemoveAt(i);
                        //for (int i = 1; i < tkns.Length; i++)
                        //{
                        //    targets.Items.Add(tkns[i]);
                        //}
                        targets.DataSource = null;
                        List<string> newSource = new List<string>();
                        for (int i = 1; i < tkns.Length; i++)
                        {
                            newSource.Add(tkns[i]);
                        }
                        targets.DataSource = newSource;
                        targets.Refresh();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("ListenPlayerAsyncError: " + e.Message);
            }
        }
    }
}
