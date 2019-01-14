using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageOrganizer {
    class Program {
        readonly NotifyIcon notifyIcon = new NotifyIcon();
        readonly ImageProcessor processor;
        readonly Icon synchronized;
        readonly Icon synchronizing;
        readonly Icon waiting;
        MenuItem exitItem;
        MenuItem stopItem;
        MenuItem startItem;
        MenuItem forceItem;
        private Program() {
            string input = ConfigurationManager.AppSettings.Get("input");
            string output = ConfigurationManager.AppSettings.Get("output");
            string rules = ConfigurationManager.AppSettings.Get("rules");
            this.processor = new ImageProcessor(input, output, rules);
            this.processor.OrganizeStart += watcher_OrganizeStart;
            this.processor.OrganizeEnd += watcher_OrganizeEnd;
            Assembly assembly = GetType().Assembly;
            this.synchronized = new Icon(assembly.GetManifestResourceStream("ImageOrganizer.Icons.vcsnormal.ico"));
            this.synchronizing = new Icon(assembly.GetManifestResourceStream("ImageOrganizer.Icons.vcsupdaterequired.ico"));
            this.waiting = new Icon(assembly.GetManifestResourceStream("ImageOrganizer.Icons.waiting.ico"));
            CreateContextMenu();
        }

        void CreateContextMenu() {
            this.exitItem = new MenuItem("Exit", (s, e) => Exit());
            this.stopItem = new MenuItem("Stop Watching", (s, e) => Stop());
            this.startItem = new MenuItem("Start Watching", (s, e) => Start());
            this.forceItem = new MenuItem("Force Synchronization", (s, e) => Force());
            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add(this.startItem);
            menu.MenuItems.Add(this.stopItem);
            menu.MenuItems.Add(this.forceItem);
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(this.exitItem);
            notifyIcon.ContextMenu = menu;
        }
        public void Run() {
            //PathTree tree = new PathTree();
            //tree.Add(@"d:\Photo\Raw\Природа\1.JPG");
            //tree.Add(@"d:\Photo\Raw\Природа\2.JPG");
            //tree.Add(@"d:\Photo\Raw\Природа\3.JPG");
            //tree.Add(@"d:\Photo\Raw\Природа\4.JPG");
            //tree.Add(@"d:\Photo\Raw\Природа\5.JPG");
            //tree.Add(@"d:\Photo\Raw\Природа\6.JPG");
            //tree.Add(@"d:\Photo\Raw\Природа\7.JPG");
            //tree.Add(@"d:\Photo\Raw\Природа\8.JPG");
            //tree.Add(@"d:\Photo\Raw\Природа\9.JPG");
            //tree.Add(@"d:\Photo\Raw\Природа\10.JPG");
            //tree.Contains(@"d:\Photo\Raw\Природа\9.JPG");
            this.notifyIcon.Visible = true;
            SetSynchronizedState();
            Start();
            Application.Run();
        }
        public void Exit() {
            this.notifyIcon.Visible = false;
            this.processor.Dispose();
            this.notifyIcon.Dispose();
            this.synchronizing.Dispose();
            this.synchronized.Dispose();
            Application.Exit();
        }
        public void Start() {
            this.processor.Start();
            this.startItem.Enabled = false;
            this.stopItem.Enabled = true;
            SetSynchronizedState();
        }
        public void Stop() {
            this.processor.Stop();
            this.startItem.Enabled = true;
            this.stopItem.Enabled = false;
            SetWaitingState();
        }
        public void Force() {
            this.processor.Force();
            SetSynchronizedState();
        }

        [STAThread]
        static void Main(string[] args) {
            bool regrun = args != null && args.Length > 0 && args[0] == "regrun";
            if (!regrun)
                TryRegistryAutorun();
            try {
                new Program().Run();
            }
            catch {
            }
        }
        static void TryRegistryAutorun() {
            try {
                string keyName = Application.ProductName;
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                key.SetValue(keyName, Application.ExecutablePath + " /regrun");
                key.Close();
            }
            catch {
            }
        }
        void watcher_OrganizeEnd(object sender, EventArgs e) {
            SetSynchronizedState();
            this.notifyIcon.ShowBalloonTip(10000, Application.ProductName, "Synchronization of images has complitted.", ToolTipIcon.Info);
        }
        void watcher_OrganizeStart(object sender, EventArgs e) {
            SetSynchronizingState();
        }
        void SetSynchronizingState() {
            this.notifyIcon.Text = "Images are synchronizing.";
            this.notifyIcon.Icon = this.synchronizing;
        }
        void SetSynchronizedState() {
            this.notifyIcon.Icon = this.synchronized;
            this.notifyIcon.Text = "Images are synchronized.";
        }
        void SetWaitingState() {
            this.notifyIcon.Text = "Synchronizing is stopped";
            this.notifyIcon.Icon = this.waiting;
        }
    }
}
