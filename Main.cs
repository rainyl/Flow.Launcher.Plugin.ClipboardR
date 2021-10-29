using System;
using System.Linq;
using System.Collections.Generic;
using Flow.Launcher.Plugin;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace Flow.Launcher.Plugin.ClipboardHistory {

    public class ClipboardHistory : IPlugin, IDisposable {

        private const int MaxDataCount = 1000;
        private readonly InputSimulator inputSimulator = new InputSimulator();
        private PluginInitContext context;
        private int currentScore = 1;
        LinkedList<ClipboardData> dataList = new LinkedList<ClipboardData>();

        public struct ClipboardData : IEquatable<ClipboardData> {
            public object data;
            public string text;
            public string displayText;
            public int score;

            public override bool Equals(object obj) => obj is ClipboardData && Equals((ClipboardData)obj);
            public bool Equals(ClipboardData other) => text.Equals(other.text);
            public override int GetHashCode() => text.GetHashCode();
        }
        
        public List<Result> Query(Query query) {
            
            IEnumerable<ClipboardData> displayData;
            if (query.Search.Trim().Length == 0) {
                displayData = dataList;
            } else {
                displayData = dataList.Where(i => i.text.ToLower().Contains(query.Search.ToLower()));
            }

            var results = new List<Result>();
            results.AddRange(displayData.Select(o => new Result {
                Title = o.displayText,
                IcoPath = "Images\\clipboard.png",
                Score = o.score,
                Action = c => {
                    if (!ClipboardMonitor.ClipboardWrapper.SetDataObject(o.data)) {
                        return false;
                    }
                    Task.Delay(50).ContinueWith(t => inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V));
                    return true;
                }
            }));
            return results;
        }

        public void Init(PluginInitContext context) {
            this.context = context;
            ClipboardMonitor.OnClipboardChange += ClipboardMonitor_OnClipboardChange;
            ClipboardMonitor.Start();
        }

        void ClipboardMonitor_OnClipboardChange(ClipboardFormat format, object data) {

            if (format == ClipboardFormat.Html ||
                format == ClipboardFormat.SymbolicLink ||
                format == ClipboardFormat.Text ||
                format == ClipboardFormat.UnicodeText) {

                if (data != null && !string.IsNullOrEmpty(data.ToString().Trim())) {
                    ClipboardData obj = new ClipboardData { };
                    obj.data = data;
                    obj.text = data.ToString();
                    obj.displayText = obj.text.Trim().Replace("\r\n", " ").Replace('\n', ' ');
                    obj.score = currentScore++ * 1000;

                    LinkedListNode<ClipboardData> node = dataList.Find(obj);
                    if (node != null) {
                        dataList.Remove(node);
                    }
                    dataList.AddFirst(obj);

                    if (dataList.Count > MaxDataCount) {
                        dataList.RemoveLast();
                    }
                }
            }
        }

        public void Dispose() {
            ClipboardMonitor.Stop();
        }
    }
}