using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Flow.Launcher.Plugin.ClipboardHistory {
    public static class ClipboardMonitor {

        public delegate void OnClipboardChangeEventHandler(ClipboardFormat format, object data);
        public static event OnClipboardChangeEventHandler OnClipboardChange;

        public static void Start() {
            ClipboardWatcher.Start();
            ClipboardWatcher.OnClipboardChange += (ClipboardFormat format, object data) => {
                if (OnClipboardChange != null) {
                    OnClipboardChange(format, data);
                }
            };
        }

        public static void Stop() {
            OnClipboardChange = null;
            ClipboardWatcher.Stop();
        }

        public static class ClipboardWrapper {
            private static T LoopCall<T>(Func<T> func, int timeout = 1) {
                T result = default(T);
                DateTimeOffset startTime = DateTimeOffset.UtcNow;
                while (true) {
                    try {
                        result = func();
                    } catch (ExternalException) {
                        Thread.Sleep(10);
                        if (DateTimeOffset.UtcNow - startTime > TimeSpan.FromSeconds(timeout))
                            return result;
                        continue;
                    }
                    break;
                }
                return result;
            }

            public static IDataObject GetDataObject() {
                return LoopCall(() => System.Windows.Forms.Clipboard.GetDataObject());
            }

            public static bool SetDataObject(object data) {
                return LoopCall(() => {
                    System.Windows.Forms.Clipboard.SetDataObject(data);
                    return true;
                });
            }
        }

        class ClipboardWatcher : Form {
            // static instance of this form
            private static ClipboardWatcher instance;
            private static Thread thread;
            private static bool running = false;

            public delegate void OnClipboardChangeEventHandler(ClipboardFormat format, object data);
            public static event OnClipboardChangeEventHandler OnClipboardChange;

            // start listening
            public static void Start() {
                // we can only have one instance if this class
                if (running) {
                    return;
                }
                running = true;
                thread = new Thread(new ParameterizedThreadStart(x => {
                    while (running) {
                        instance = new ClipboardWatcher();
                        Application.Run(instance);
                    }
                }));
                thread.SetApartmentState(ApartmentState.STA); // give the [STAThread] attribute
                thread.IsBackground = true;
                thread.Start();
            }

            // stop listening (dispose form)
            public static void Stop() {
                if (!running) {
                    return;
                }
                running = false;
                if (instance != null) {
                    instance.Invoke(new MethodInvoker(instance.Close));
                    instance.Dispose();
                    instance = null;
                }
                if (thread != null) {
                    thread.Join();
                    thread = null;
                }
            }

            private ClipboardWatcher() {
                this.ShowInTaskbar = false;
            }

            const int WM_CLIPBOARDUPDATE = 0x031D;
            static IntPtr HWND_MESSAGE = new IntPtr(-3);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool AddClipboardFormatListener(IntPtr hwnd);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

            [DllImport("user32.dll", SetLastError = true)]
            static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

            protected override void OnHandleCreated(EventArgs e) {
                base.OnHandleCreated(e);
                try {
                    SetParent(this.Handle, HWND_MESSAGE);
                    AddClipboardFormatListener(this.Handle);
                } catch {
                }
            }

            protected override void OnHandleDestroyed(EventArgs e) {
                try {
                    RemoveClipboardFormatListener(this.Handle);
                } catch {
                }
                base.OnHandleDestroyed(e);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_CLIPBOARDUPDATE) {
                    ClipChanged();
                }
                base.WndProc(ref m);
            }


            static readonly string[] formats = Enum.GetNames(typeof(ClipboardFormat));

            private void ClipChanged()
            {
                IDataObject iData = ClipboardWrapper.GetDataObject();
                if (iData == null) {
                    return;
                }

                ClipboardFormat? format = null;
                foreach (var f in formats) {
                    if (iData.GetDataPresent(f)) {
                        format = (ClipboardFormat)Enum.Parse(typeof(ClipboardFormat), f);
                        break;
                    }
                }

                object data = iData.GetData(format.ToString());
                if (data == null || format == null) {
                    return;
                }

                if (OnClipboardChange != null) {
                    OnClipboardChange((ClipboardFormat)format, data);
                }
            }

        }
    }

    public enum ClipboardFormat : byte {
        /// <summary>Specifies the standard ANSI text format. This static field is read-only.
        /// </summary>
        /// <filterpriority>1</filterpriority>
        Text,
        /// <summary>Specifies the standard Windows Unicode text format. This static field
        /// is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        UnicodeText,
        /// <summary>Specifies the Windows device-independent bitmap (DIB) format. This static
        /// field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        Dib,
        /// <summary>Specifies a Windows bitmap format. This static field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        Bitmap,
        /// <summary>Specifies the Windows enhanced metafile format. This static field is
        /// read-only.</summary>
        /// <filterpriority>1</filterpriority>
        EnhancedMetafile,
        /// <summary>Specifies the Windows metafile format, which Windows Forms does not
        /// directly use. This static field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        MetafilePict,
        /// <summary>Specifies the Windows symbolic link format, which Windows Forms does
        /// not directly use. This static field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        SymbolicLink,
        /// <summary>Specifies the Windows Data Interchange Format (DIF), which Windows Forms
        /// does not directly use. This static field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        Dif,
        /// <summary>Specifies the Tagged Image File Format (TIFF), which Windows Forms does
        /// not directly use. This static field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        Tiff,
        /// <summary>Specifies the standard Windows original equipment manufacturer (OEM)
        /// text format. This static field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        OemText,
        /// <summary>Specifies the Windows palette format. This static field is read-only.
        /// </summary>
        /// <filterpriority>1</filterpriority>
        Palette,
        /// <summary>Specifies the Windows pen data format, which consists of pen strokes
        /// for handwriting software, Windows Forms does not use this format. This static
        /// field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        PenData,
        /// <summary>Specifies the Resource Interchange File Format (RIFF) audio format,
        /// which Windows Forms does not directly use. This static field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        Riff,
        /// <summary>Specifies the wave audio format, which Windows Forms does not directly
        /// use. This static field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        WaveAudio,
        /// <summary>Specifies the Windows file drop format, which Windows Forms does not
        /// directly use. This static field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        FileDrop,
        /// <summary>Specifies the Windows culture format, which Windows Forms does not directly
        /// use. This static field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        Locale,
        /// <summary>Specifies text consisting of HTML data. This static field is read-only.
        /// </summary>
        /// <filterpriority>1</filterpriority>
        Html,
        /// <summary>Specifies text consisting of Rich Text Format (RTF) data. This static
        /// field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        Rtf,
        /// <summary>Specifies a comma-separated value (CSV) format, which is a common interchange
        /// format used by spreadsheets. This format is not used directly by Windows Forms.
        /// This static field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        CommaSeparatedValue,
        /// <summary>Specifies the Windows Forms string class format, which Windows Forms
        /// uses to store string objects. This static field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        StringFormat,
        /// <summary>Specifies a format that encapsulates any type of Windows Forms object.
        /// This static field is read-only.</summary>
        /// <filterpriority>1</filterpriority>
        Serializable,
    }
}