using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTools {
    public class Arguments {
        enum ReadArgumentsState {
            Start,
            WaitInputDirectory,
            WaitOutputDirectory,
            WaitRules
        }

        public string InputDirectory { get; set; }
        public string OutputDirectory { get; set; }
        public string RulesPath { get; set; }
        public bool IsEmpty {
            get {
                return InputDirectory == null && OutputDirectory == null && RulesPath == null;
            }
        }

        public void Read(string[] args) {
            InputDirectory = null;
            OutputDirectory = null;
            RulesPath = null;
            ReadArgumentsState state = ReadArgumentsState.Start;
            if (args != null && args.Length > 0) {
                int length = args.Length;
                for (int index = 0; index < length; index++) {
                    string arg = args[index];
                    switch (arg) {
                        case "/o":
                            state = ReadArgumentsState.WaitOutputDirectory;
                            break;
                        case "/r":
                            state = ReadArgumentsState.WaitRules;
                            break;
                        case "/i":
                            state = ReadArgumentsState.WaitInputDirectory;
                            break;
                        default:
                            if (state == ReadArgumentsState.WaitOutputDirectory)
                                OutputDirectory = arg;
                            else if (state == ReadArgumentsState.WaitRules) {
                                if (File.Exists(arg))
                                    RulesPath = File.ReadAllText(arg);
                                else
                                    RulesPath = arg;
                            }
                            else
                                InputDirectory = arg;
                            state = ReadArgumentsState.Start;
                            break;
                    }
                }
            }
        }
    }
}
