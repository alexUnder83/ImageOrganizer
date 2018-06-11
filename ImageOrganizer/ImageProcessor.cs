using ImageTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageOrganizer {
    public class ImageProcessor : IDisposable {
        static readonly HashSet<string> imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "jpg",
            "png",
            "gif",
            "mov"
        };
        HashSet<string> filesToProcess = new HashSet<string>();
        FileSystemWatcher watcher = new FileSystemWatcher();
        string outputDir;
        string inputDir;
        string rules;
        CancellationTokenSource tokenSource = new CancellationTokenSource();
        ManualResetEvent createdEvent = new ManualResetEvent(false);
        ManualResetEvent changedEvent = new ManualResetEvent(false);
        Task task;
        string currentFileName;
        readonly object lockObject = new object();
        public ImageProcessor(string inputDir, string outputDir, string rules) {
            this.inputDir = inputDir;
            this.outputDir = outputDir;
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
            if (!Directory.Exists(inputDir)) {
                AppendLog(string.Format("{0}: launching failed. {1} isn't exists.", DateTime.Now, inputDir));
                throw new ArgumentException();
            }
            this.rules = rules;
            this.watcher.Path = inputDir;
            this.watcher.IncludeSubdirectories = true;
            this.watcher.NotifyFilter = NotifyFilters.LastWrite;
            this.watcher.Changed += watcher_Changed;
            this.task = StartTask();
        }

        public event EventHandler OrganizeStart;
        public event EventHandler OrganizeEnd;

        void watcher_Changed(object sender, FileSystemEventArgs e) {
            if (!IsSupportedFile(e.FullPath) || this.filesToProcess.Contains(e.FullPath))
                return;

            if (!File.Exists(e.FullPath)) {
                if (IsFile(e.FullPath)) {
                    OnFileCreated();
                    this.currentFileName = null;
                }
                return;
            }

            if (this.currentFileName != e.FullPath)
                this.currentFileName = null;

            if (string.IsNullOrEmpty(this.currentFileName)) {
                this.currentFileName = e.FullPath;
                OnFileCreating();
            }
            else {
                lock (this.lockObject) {
                    this.filesToProcess.Add(this.currentFileName);
                }
                OnFileCreated();
                this.currentFileName = null;
            }
        }
        void OnFileCreated() {
            this.createdEvent.Reset();
            this.changedEvent.Set();
        }
        void OnFileCreating() {
            this.changedEvent.Reset();
            this.createdEvent.Set();
        }
        bool IsSupportedFile(string path) {
            return imageExtensions.Contains(Path.GetExtension(path).TrimStart('.'));
        }
        bool IsFile(string path) {
            return !string.Equals(path, Path.GetDirectoryName(path), StringComparison.Ordinal);
        }
        Task StartTask() {
            CancellationToken token = this.tokenSource.Token;
            return Task.Factory.StartNew(() => {
                bool isProcessStart = false;
                while (!token.IsCancellationRequested) {
                    if (!isProcessStart) {
                        createdEvent.WaitOne();
                        if (!token.IsCancellationRequested)
                            BeforeProcessFiles();
                        isProcessStart = true;
                        changedEvent.WaitOne();
                    }
                    else {
                        if (!createdEvent.WaitOne(15000)) {
                            isProcessStart = false;
                            if (!token.IsCancellationRequested) {
                                ProcessFiles(token);
                                if (this.filesToProcess.Count == 0)
                                    AfterProcessFiles();
                            }
                        }
                        else
                            changedEvent.WaitOne();
                    }
                }
            }, token);
        }
        public void Start() {
            this.watcher.EnableRaisingEvents = true;
        }
        public void Stop() {
            this.watcher.EnableRaisingEvents = false;
        }
        public void Force() {
            lock (this.lockObject) {
                CancellationToken token = this.tokenSource.Token;
                string[] files = Directory.GetFiles(this.inputDir, "*.*", SearchOption.AllDirectories);
                ProcessFiles(files, token);
                AppendLog(files.Length + " files has been copied.");
            }
        }
        void ProcessFiles(CancellationToken token) {
            lock (this.lockObject) {
                ProcessFiles(this.filesToProcess, token);
                AppendLog(this.filesToProcess.Count + " files has been copied.");
                this.filesToProcess.Clear();
            }
        }
        void ProcessFiles(IEnumerable<string> files, CancellationToken token) {
            foreach (string fullName in files) {
                if (token.IsCancellationRequested)
                    break;
                string imagePath = ImagePathBuilder.BuildPath(this.outputDir, fullName, this.rules);
                if (!File.Exists(imagePath) && File.Exists(fullName)) {
                    try {
                        string directory = Path.GetDirectoryName(imagePath);
                        if (!Directory.Exists(directory))
                            Directory.CreateDirectory(directory);
                        File.Copy(fullName, imagePath, true);
                    }
                    catch (Exception e) {
                        AppendLog("The file " + fullName + " has not copied.", e.Message, e.StackTrace);
                    }
                }
            }
        }
        void AfterProcessFiles() {
            if (OrganizeEnd != null)
                OrganizeEnd(this, EventArgs.Empty);
            AppendLog(DateTime.Now.ToString() + ": end files processing");
        }
        void BeforeProcessFiles() {
            if (OrganizeStart != null)
                OrganizeStart(this, EventArgs.Empty);
            AppendLog(DateTime.Now.ToString() + ": start files processing");
        }
        void AppendLog(params string[] content) {
            File.AppendAllLines(Path.Combine(outputDir, "Log.txt"), content);
        }
        public void Dispose() {
            if (this.watcher != null) {
                this.watcher.Dispose();
                this.watcher = null;
            }
            if (this.task != null) {
                this.tokenSource.Cancel();
                this.createdEvent.Set();
                this.changedEvent.Set();
                this.task.Wait();
                this.task.Dispose();
                this.task = null;
                this.tokenSource.Dispose();
                this.createdEvent.Dispose();
                this.changedEvent.Dispose();
            }
        }
    }
}
