using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;

namespace EasyBlueStudio {
    public class App : Application {
        [STAThread]
        public static void Main() {
            bool isNew;
            using (var mutex = new Mutex(true, "EasyBlueStudio_SingleInstance_Mutex_2026", out isNew)) {
                if (!isNew) {
                    MessageBox.Show("EasyBlue Studio가 이미 실행 중입니다.\n작업 표시줄 오른쪽 시스템 트레이에서 아이콘을 확인하세요.", "EasyBlue Studio", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var app = new App();
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                var win = new MainWindow();
                app.Run(win);
            }
        }
    }

    // =========================================================================
    // CONFIGURATION MODEL
    // =========================================================================
    public class StudioConfig {
        public string PhoneEdge = "Right"; // Left, Right, Top, Bottom
        public bool EdgeTraversalEnabled = true;
        public bool EdgeBounceReturnEnabled = true;
        public double MouseSpeed = 1.0;
        public int EdgeDeadzonePixels = 8;
        public int InputMode = 0; // 0: Combo (KB+Mouse), 1: Keyboard Only, 2: Mouse Only
        public bool LedEnabled = true;
        public int PasteDelayMs = 15;
        public bool AutoStartWithWindows = false;
        public bool StartMinimized = false;

        // Hotkeys
        public string HotkeySwitch = "Ctrl+Alt+Right";
        public string HotkeyReturn = "Ctrl+Alt+Left";
        public string HotkeyDisconnect = "Ctrl+Alt+End";
        public string HotkeySlot1 = "Ctrl+F1";
        public string HotkeySlot2 = "Ctrl+F2";
        public string HotkeySlot3 = "Ctrl+F3";
        public string HotkeySlot4 = "Ctrl+F4";
        public string HotkeyPaste = "Ctrl+Alt+V";

        // Paired Device Slots (Up to 4 devices)
        public string[] DeviceSlots = new string[] {
            "스마트폰 1 (주 기기)",
            "태블릿 2 (보조 기기)",
            "스마트폰 3 (슬롯 3)",
            "디바이스 4 (슬롯 4)"
        };
        public int ActiveSlotIndex = 0;

        private static string ConfigPath {
            get {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dir = System.IO.Path.Combine(appData, "EasyBlueStudio");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return System.IO.Path.Combine(dir, "config.ini");
            }
        }

        public static StudioConfig Load() {
            var cfg = new StudioConfig();
            try {
                if (File.Exists(ConfigPath)) {
                    string[] lines = File.ReadAllLines(ConfigPath, Encoding.UTF8);
                    foreach (string rawLine in lines) {
                        string line = rawLine.Trim();
                        if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith(";")) continue;
                        int eqIdx = line.IndexOf('=');
                        if (eqIdx <= 0) continue;
                        string key = line.Substring(0, eqIdx).Trim();
                        string val = line.Substring(eqIdx + 1).Trim();

                        if (key.Equals("PhoneEdge", StringComparison.OrdinalIgnoreCase)) cfg.PhoneEdge = val;
                        else if (key.Equals("EdgeTraversalEnabled", StringComparison.OrdinalIgnoreCase)) bool.TryParse(val, out cfg.EdgeTraversalEnabled);
                        else if (key.Equals("EdgeBounceReturnEnabled", StringComparison.OrdinalIgnoreCase)) bool.TryParse(val, out cfg.EdgeBounceReturnEnabled);
                        else if (key.Equals("MouseSpeed", StringComparison.OrdinalIgnoreCase)) double.TryParse(val, out cfg.MouseSpeed);
                        else if (key.Equals("InputMode", StringComparison.OrdinalIgnoreCase)) int.TryParse(val, out cfg.InputMode);
                        else if (key.Equals("LedEnabled", StringComparison.OrdinalIgnoreCase)) bool.TryParse(val, out cfg.LedEnabled);
                        else if (key.Equals("HotkeySwitch", StringComparison.OrdinalIgnoreCase)) cfg.HotkeySwitch = val;
                        else if (key.Equals("HotkeyReturn", StringComparison.OrdinalIgnoreCase)) cfg.HotkeyReturn = val;
                        else if (key.Equals("HotkeyDisconnect", StringComparison.OrdinalIgnoreCase)) cfg.HotkeyDisconnect = val;
                        else if (key.Equals("HotkeySlot1", StringComparison.OrdinalIgnoreCase)) cfg.HotkeySlot1 = val;
                        else if (key.Equals("HotkeySlot2", StringComparison.OrdinalIgnoreCase)) cfg.HotkeySlot2 = val;
                        else if (key.Equals("HotkeySlot3", StringComparison.OrdinalIgnoreCase)) cfg.HotkeySlot3 = val;
                        else if (key.Equals("HotkeySlot4", StringComparison.OrdinalIgnoreCase)) cfg.HotkeySlot4 = val;
                        else if (key.Equals("HotkeyPaste", StringComparison.OrdinalIgnoreCase)) cfg.HotkeyPaste = val;
                        else if (key.Equals("Slot1Name", StringComparison.OrdinalIgnoreCase)) cfg.DeviceSlots[0] = val;
                        else if (key.Equals("Slot2Name", StringComparison.OrdinalIgnoreCase)) cfg.DeviceSlots[1] = val;
                        else if (key.Equals("Slot3Name", StringComparison.OrdinalIgnoreCase)) cfg.DeviceSlots[2] = val;
                        else if (key.Equals("Slot4Name", StringComparison.OrdinalIgnoreCase)) cfg.DeviceSlots[3] = val;
                        else if (key.Equals("ActiveSlotIndex", StringComparison.OrdinalIgnoreCase)) int.TryParse(val, out cfg.ActiveSlotIndex);
                    }
                }
            } catch { }
            return cfg;
        }

        public void Save() {
            try {
                var sb = new StringBuilder();
                sb.AppendLine("# EasyBlue Studio 2026 Configuration");
                sb.AppendLine("PhoneEdge=" + PhoneEdge);
                sb.AppendLine("EdgeTraversalEnabled=" + EdgeTraversalEnabled);
                sb.AppendLine("EdgeBounceReturnEnabled=" + EdgeBounceReturnEnabled);
                sb.AppendLine("MouseSpeed=" + MouseSpeed.ToString("0.0"));
                sb.AppendLine("InputMode=" + InputMode);
                sb.AppendLine("LedEnabled=" + LedEnabled);
                sb.AppendLine("HotkeySwitch=" + HotkeySwitch);
                sb.AppendLine("HotkeyReturn=" + HotkeyReturn);
                sb.AppendLine("HotkeyDisconnect=" + HotkeyDisconnect);
                sb.AppendLine("HotkeySlot1=" + HotkeySlot1);
                sb.AppendLine("HotkeySlot2=" + HotkeySlot2);
                sb.AppendLine("HotkeySlot3=" + HotkeySlot3);
                sb.AppendLine("HotkeySlot4=" + HotkeySlot4);
                sb.AppendLine("HotkeyPaste=" + HotkeyPaste);
                sb.AppendLine("Slot1Name=" + DeviceSlots[0]);
                sb.AppendLine("Slot2Name=" + DeviceSlots[1]);
                sb.AppendLine("Slot3Name=" + DeviceSlots[2]);
                sb.AppendLine("Slot4Name=" + DeviceSlots[3]);
                sb.AppendLine("ActiveSlotIndex=" + ActiveSlotIndex);
                File.WriteAllText(ConfigPath, sb.ToString(), Encoding.UTF8);
            } catch { }
        }
    }

    // =========================================================================
    // LOW-LEVEL HID HARDWARE CONTROLLER (Direct Zaram EasyBlue Protocol)
    // =========================================================================
    public class EasyBlueHardware : IDisposable {
        [DllImport("hid.dll", SetLastError = true)]
        private static extern void HidD_GetHidGuid(out Guid hidGuid);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, string enumerator, IntPtr hwndParent, uint flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr deviceInfoSet, IntPtr deviceInfoData, ref Guid interfaceClassGuid, uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize, ref uint requiredSize, IntPtr deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_SetFeature(IntPtr hidDeviceObject, byte[] lpReportBuffer, int reportBufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetFeature(IntPtr hidDeviceObject, byte[] lpReportBuffer, int reportBufferLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, ref NativeOverlapped lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetOverlappedResult(IntPtr hFile, ref NativeOverlapped lpOverlapped, out uint lpNumberOfBytesTransferred, bool bWait);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ResetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CancelIo(IntPtr hFile);

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA {
            public uint cbSize;
            public Guid interfaceClassGuid;
            public uint flags;
            public IntPtr reserved;
        }

        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint DIGCF_DEVICEINTERFACE = 0x00000010;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_FLAG_OVERLAPPED = 0x40000000;

        private IntPtr _hidHandle = IntPtr.Zero;
        private IntPtr _hidEvent = IntPtr.Zero;
        private readonly object _ioLock = new object();
        private bool _isConnected = false;
        private bool _isPureHidMode = false;
        private string _devicePath = null;

        public bool IsConnected { get { return _isConnected; } }
        public bool IsPureHidMode { get { return _isPureHidMode; } }
        public string DevicePath { get { return _devicePath; } }

        public bool Connect() {
            Close();
            Guid hidGuid;
            HidD_GetHidGuid(out hidGuid);
            IntPtr devInfo = SetupDiGetClassDevs(ref hidGuid, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (devInfo == (IntPtr)(-1)) return false;

            try {
                SP_DEVICE_INTERFACE_DATA ifData = new SP_DEVICE_INTERFACE_DATA();
                ifData.cbSize = (uint)Marshal.SizeOf(ifData);

                for (uint i = 0; SetupDiEnumDeviceInterfaces(devInfo, IntPtr.Zero, ref hidGuid, i, ref ifData); i++) {
                    uint reqSize = 0;
                    SetupDiGetDeviceInterfaceDetail(devInfo, ref ifData, IntPtr.Zero, 0, ref reqSize, IntPtr.Zero);
                    if (reqSize == 0) continue;

                    IntPtr detailData = Marshal.AllocHGlobal((int)reqSize);
                    try {
                        Marshal.WriteInt32(detailData, IntPtr.Size == 8 ? 8 : 5);
                        if (SetupDiGetDeviceInterfaceDetail(devInfo, ref ifData, detailData, reqSize, ref reqSize, IntPtr.Zero)) {
                            IntPtr pDevicePath = new IntPtr(detailData.ToInt64() + 4);
                            string path = Marshal.PtrToStringAuto(pDevicePath);
                            if (path != null && path.IndexOf("vid_1d54&pid_b002", StringComparison.OrdinalIgnoreCase) >= 0) {
                                IntPtr handle = CreateFile(path, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, IntPtr.Zero);
                                if (handle != IntPtr.Zero && handle != (IntPtr)(-1)) {
                                    _hidHandle = handle;
                                    _hidEvent = CreateEvent(IntPtr.Zero, true, false, null);
                                    _devicePath = path;
                                    _isConnected = true;
                                    CheckSecurityDrives();
                                    WakeUpRadio();
                                    return true;
                                }
                            }
                        }
                    } finally {
                        Marshal.FreeHGlobal(detailData);
                    }
                }
            } finally {
                SetupDiDestroyDeviceInfoList(devInfo);
            }

            _isConnected = false;
            return false;
        }

        private void CheckSecurityDrives() {
            _isPureHidMode = true;
            try {
                foreach (var drive in DriveInfo.GetDrives()) {
                    if (drive.IsReady && drive.DriveType == DriveType.Removable) {
                        if (drive.VolumeLabel.IndexOf("Easy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            drive.VolumeLabel.IndexOf("EBTiny", StringComparison.OrdinalIgnoreCase) >= 0) {
                            _isPureHidMode = false;
                            break;
                        }
                    }
                }
            } catch { }
        }

        public bool SendCommand(ushort cmdCode, ushort subParam, byte[] extraData, byte reportId = 0x01) {
            if (_hidHandle == IntPtr.Zero) return false;

            // EasyBlue vendor HID feature report: 513 bytes
            byte[] packet = new byte[513];
            packet[0] = reportId; // Report ID (0x01, 0x02, or 0x03)
            packet[1] = 0xBF; // Magic little-endian
            packet[2] = 0x2A;
            packet[3] = 0x38; // Payload length (56 bytes)
            packet[4] = 0x00;
            packet[5] = (byte)(cmdCode & 0xFF);
            packet[6] = (byte)((cmdCode >> 8) & 0xFF);
            packet[7] = (byte)(subParam & 0xFF);
            packet[8] = (byte)((subParam >> 8) & 0xFF);

            if (extraData != null && extraData.Length > 0) {
                int copyLen = Math.Min(extraData.Length, 500);
                Array.Copy(extraData, 0, packet, 9, copyLen);
            }

            return HidD_SetFeature(_hidHandle, packet, packet.Length);
        }

        public bool SendRawFeatureReport(byte[] report) {
            if (_hidHandle == IntPtr.Zero || report == null) return false;
            byte[] packet = new byte[513];
            Array.Copy(report, 0, packet, 0, Math.Min(report.Length, 513));
            return HidD_SetFeature(_hidHandle, packet, packet.Length);
        }

        public bool ClearPairingSlot(int slotIndex) {
            if (slotIndex < 0 || slotIndex > 4) return false;
            ushort cmd = (ushort)(0x8200 | slotIndex);
            return SendCommand(cmd, 0x0000, null, 0x01);
        }

        public bool ClearAllPairings() {
            bool ok = true;
            for (int s = 1; s <= 4; s++) {
                ClearPairingSlot(s);
                Thread.Sleep(30);
            }
            ClearPairingSlot(0);
            return ok;
        }

        private bool SendVer21Packet(byte reportId, ushort cmd, ushort sub, ushort lenWord, ushort tag, byte[] extra = null, byte[] responseOut = null) {
            if (_hidHandle == IntPtr.Zero) return false;
            lock (_ioLock) {
                byte[] packet = new byte[513];
                packet[0] = reportId;
                packet[1] = (byte)(cmd & 0xFF);
                packet[2] = (byte)(cmd >> 8);
                packet[3] = (byte)(sub & 0xFF);
                packet[4] = (byte)(sub >> 8);
                packet[5] = (byte)(lenWord & 0xFF);
                packet[6] = (byte)(lenWord >> 8);
                packet[7] = (byte)(tag & 0xFF);
                packet[8] = (byte)(tag >> 8);
                if (extra != null && extra.Length > 0) {
                    Array.Copy(extra, 0, packet, 9, Math.Min(extra.Length, 500));
                }

                byte[] inBuf = responseOut ?? new byte[513];
                NativeOverlapped ov = new NativeOverlapped();
                bool hasOv = (_hidEvent != IntPtr.Zero);
                bool readPending = false;
                uint bytesRead = 0;

                if (hasOv) {
                    ResetEvent(_hidEvent);
                    ov.EventHandle = _hidEvent;
                    bool rOk = ReadFile(_hidHandle, inBuf, (uint)inBuf.Length, out bytesRead, ref ov);
                    if (!rOk && Marshal.GetLastWin32Error() == 997) {
                        readPending = true;
                    }
                }

                bool setOk = HidD_SetFeature(_hidHandle, packet, packet.Length);

                if (readPending) {
                    if (WaitForSingleObject(_hidEvent, 250) == 0) {
                        GetOverlappedResult(_hidHandle, ref ov, out bytesRead, false);
                    } else {
                        CancelIo(_hidHandle);
                    }
                }
                return setOk;
            }
        }

        public ushort GetBcmStatus() {
            byte[] resp = new byte[513];
            if (SendVer21Packet(0x01, 0xEB01, 0xFFFF, 0x0000, 0x0000, null, resp)) {
                if (resp[1] == 0x01 && resp[2] == 0xEB && resp[3] == 0xFF && resp[4] == 0xFF && resp[5] == 0x01) {
                    return (ushort)(resp[9] | (resp[10] << 8));
                }
            }
            return 0;
        }

        private bool _radioAwake = false;

        public void WakeUpRadio() {
            if (_hidHandle == IntPtr.Zero) return;
            // Ver 2.1 Slot 4: Power-cycle / initialize Broadcom Bluetooth chip (0xEB01 / 0x7FFF / 0 -> 1)
            SendVer21Packet(0x01, 0xEB01, 0x7FFF, 0x0000, 0x0001, new byte[] { 0x00, 0x00 });
            Thread.Sleep(50);
            SendVer21Packet(0x01, 0xEB01, 0x7FFF, 0x0000, 0x0001, new byte[] { 0x01, 0x00 });
            Thread.Sleep(350);
            _radioAwake = true;
        }

        public bool EnterPairingMode(int targetSlot = 0) {
            if (_hidHandle == IntPtr.Zero) return false;

            // 1. Ensure Broadcom BT radio is initialized (wBcmStat bit 15 = 1)
            if (!_radioAwake) {
                WakeUpRadio();
            }

            // 2. Disconnect active BT link if any (Slot 11: Device_CommDisc)
            SendVer21Packet(0x01, 0xEB01, 0x8000, 0x0000, 0x0001, new byte[] { 0x00, 0xC0 });
            Thread.Sleep(100);

            // 3. Trigger Discoverable / Pairing Mode (Slot 6: 0x23C0 -> 200ms -> 0x23C1, wBcmStat -> 0x8002)
            bool ok1 = SendVer21Packet(0x01, 0xEB01, 0x0000, 0x0000, 0x0001, new byte[] { 0xC0, 0x23 });
            Thread.Sleep(200);
            bool ok2 = SendVer21Packet(0x01, 0xEB01, 0x0000, 0x0000, 0x0001, new byte[] { 0xC1, 0x23 });

            // 4. Legacy Ver 1.0 fallback (0x2ABF) for PID_B001 compatibility
            SendCommand(0x8300, 0x0000, null, 0x01);
            return ok1 || ok2;
        }

        public bool SelectDeviceSlot(int slotIndex) {
            byte[] slotData = new byte[] { (byte)slotIndex };
            return SendCommand(0x8310, (ushort)slotIndex, slotData, 0x01);
        }

        public bool DisconnectDevice() {
            // Ver 2.1 Device_CommDisc (Slot 11) + Ver 1.0 fallback
            bool ok = SendVer21Packet(0x01, 0xEB01, 0x8000, 0x0000, 0x0001, new byte[] { 0x00, 0xC0 });
            SendCommand(0x8100, 0x0002, null);
            return ok;
        }

        public bool SetLed(bool on) {
            return SendCommand(0x0011, (ushort)(on ? 1 : 0), null);
        }

        public bool ClearPairings() {
            return SendCommand(0x8200, 0x0000, null);
        }

        public bool SetInputMode(int mode) {
            return SendCommand(0x8021, (ushort)mode, null);
        }

        public bool SendMouseReport(byte buttons, sbyte dx, sbyte dy, sbyte wheel) {
            if (_hidHandle == IntPtr.Zero) return false;
            byte[] report = new byte[513];
            report[0] = 0x02; // Mouse Report ID
            report[1] = buttons;
            report[2] = (byte)dx;
            report[3] = (byte)dy;
            report[4] = (byte)wheel;
            return HidD_SetFeature(_hidHandle, report, report.Length);
        }

        private byte _lastHidKey = 0;

        public bool SendHidKeyPulse(byte hidKey) {
            if (_hidHandle == IntPtr.Zero || hidKey == 0) return false;
            // Ver 2.1 Slot 6: 0x2400 | hidKey (Key Down) -> 40ms -> 0x2500 | hidKey (Key Up)
            bool okDown = SendVer21Packet(0x01, 0xEB01, 0x0000, 0x0000, 0x0001, new byte[] { hidKey, 0x24 });
            Thread.Sleep(40);
            bool okUp = SendVer21Packet(0x01, 0xEB01, 0x0000, 0x0000, 0x0001, new byte[] { hidKey, 0x25 });
            Thread.Sleep(40);
            return okDown || okUp;
        }

        public bool SendPinCode(string pin) {
            if (_hidHandle == IntPtr.Zero || string.IsNullOrEmpty(pin)) return false;
            foreach (char c in pin.Trim()) {
                byte hid = 0;
                if (c >= '1' && c <= '9') hid = (byte)(0x1E + (c - '1'));
                else if (c == '0') hid = 0x27;
                if (hid != 0) {
                    SendHidKeyPulse(hid);
                }
            }
            // Send Enter (HID 0x28)
            return SendHidKeyPulse(0x28);
        }

        public bool SendKeyboardReport(byte modifier, byte[] keycodes) {
            if (_hidHandle == IntPtr.Zero) return false;
            byte key = (keycodes != null && keycodes.Length > 0) ? keycodes[0] : (byte)0;
            if (key != 0) {
                _lastHidKey = key;
                SendVer21Packet(0x01, 0xEB01, 0x0000, 0x0000, 0x0001, new byte[] { key, 0x24 });
            } else if (_lastHidKey != 0) {
                SendVer21Packet(0x01, 0xEB01, 0x0000, 0x0000, 0x0001, new byte[] { _lastHidKey, 0x25 });
                _lastHidKey = 0;
            }
            // Also send legacy report for Ver 1.0 compatibility
            byte[] report = new byte[513];
            report[0] = 0x01; // Keyboard Report ID
            report[1] = modifier;
            report[2] = 0x00; // Reserved
            if (keycodes != null) {
                for (int i = 0; i < Math.Min(keycodes.Length, 6); i++) {
                    report[3 + i] = keycodes[i];
                }
            }
            return HidD_SetFeature(_hidHandle, report, report.Length);
        }

        public void Close() {
            if (_hidHandle != IntPtr.Zero && _hidHandle != (IntPtr)(-1)) {
                try { CloseHandle(_hidHandle); } catch { }
                _hidHandle = IntPtr.Zero;
            }
            _isConnected = false;
        }

        public void Dispose() {
            Close();
        }
    }

    // =========================================================================
    // GLOBAL INPUT HOOK & PC <-> MOBILE CONTROLLER
    // =========================================================================
    public class EasyBlueInputHook : IDisposable {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;
        private const uint LLMHF_INJECTED = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_MOUSEWHEEL = 0x020A;

        private LowLevelProc _mouseDelegate;
        private LowLevelProc _keyboardDelegate;
        private IntPtr _mouseHook = IntPtr.Zero;
        private IntPtr _keyboardHook = IntPtr.Zero;

        private bool _isMobileActive = false;
        private POINT _lastMousePos;
        private int _escCount = 0;
        private DateTime _lastEscTime = DateTime.MinValue;

        public bool IsMobileActive { get { return _isMobileActive; } }

        public event Action<bool> OnModeChanged; // true: Mobile, false: PC
        public event Action<string> OnHotkeyTriggered;

        private StudioConfig _config;
        private EasyBlueHardware _hardware;

        public EasyBlueInputHook(StudioConfig config, EasyBlueHardware hw) {
            _config = config;
            _hardware = hw;
        }

        public void Install() {
            Uninstall();
            _mouseDelegate = MouseHookCallback;
            _keyboardDelegate = KeyboardHookCallback;

            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule) {
                IntPtr hMod = GetModuleHandle(curModule.ModuleName);
                _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardDelegate, hMod, 0);
                _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseDelegate, hMod, 0);
            }
        }

        public void SwitchToMobile() {
            if (_isMobileActive) return;
            _isMobileActive = true;
            GetCursorPos(out _lastMousePos);
            if (OnModeChanged != null) OnModeChanged(true);
        }

        public void SwitchToPC() {
            if (!_isMobileActive) return;
            _isMobileActive = false;
            SetCursorPos(_lastMousePos.x, _lastMousePos.y);
            if (OnModeChanged != null) OnModeChanged(false);
        }

        public void ToggleMode() {
            if (_isMobileActive) SwitchToPC();
            else SwitchToMobile();
        }

        private bool CheckHotkeyDown(string hotkeyStr, uint currentVk) {
            if (string.IsNullOrEmpty(hotkeyStr)) return false;
            string[] parts = hotkeyStr.Split('+');
            bool reqCtrl = false, reqAlt = false, reqShift = false;
            string keyPart = "";

            foreach (var p in parts) {
                string part = p.Trim();
                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)) reqCtrl = true;
                else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase)) reqAlt = true;
                else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase)) reqShift = true;
                else keyPart = part;
            }

            bool ctrlDown = (GetAsyncKeyState(0x11) & 0x8000) != 0;
            bool altDown = (GetAsyncKeyState(0x12) & 0x8000) != 0;
            bool shiftDown = (GetAsyncKeyState(0x10) & 0x8000) != 0;

            if (reqCtrl != ctrlDown || reqAlt != altDown || reqShift != shiftDown) return false;

            if (string.IsNullOrEmpty(keyPart)) return false;
            if (keyPart.Equals("Right", StringComparison.OrdinalIgnoreCase) && currentVk == 0x27) return true;
            if (keyPart.Equals("Left", StringComparison.OrdinalIgnoreCase) && currentVk == 0x25) return true;
            if (keyPart.Equals("Up", StringComparison.OrdinalIgnoreCase) && currentVk == 0x26) return true;
            if (keyPart.Equals("Down", StringComparison.OrdinalIgnoreCase) && currentVk == 0x28) return true;
            if (keyPart.Equals("End", StringComparison.OrdinalIgnoreCase) && currentVk == 0x23) return true;
            if (keyPart.Equals("ScrollLock", StringComparison.OrdinalIgnoreCase) && currentVk == 0x91) return true;
            if (keyPart.Equals("Pause", StringComparison.OrdinalIgnoreCase) && currentVk == 0x13) return true;
            if (keyPart.StartsWith("F", StringComparison.OrdinalIgnoreCase)) {
                int fNum;
                if (int.TryParse(keyPart.Substring(1), out fNum) && fNum >= 1 && fNum <= 12) {
                    if (currentVk == (uint)(0x6F + fNum)) return true;
                }
            }
            if (keyPart.Length == 1 && ((char)currentVk == char.ToUpper(keyPart[0]))) return true;

            return false;
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)) {
                KBDLLHOOKSTRUCT kbd = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

                // Emergency Return: ESC 3 times within 1.5s OR Pause/Break
                if (kbd.vkCode == 0x1B) { // ESC
                    if ((DateTime.Now - _lastEscTime).TotalSeconds < 1.5) {
                        _escCount++;
                        if (_escCount >= 3) {
                            _escCount = 0;
                            SwitchToPC();
                            return (IntPtr)1;
                        }
                    } else {
                        _escCount = 1;
                    }
                    _lastEscTime = DateTime.Now;
                } else if (kbd.vkCode == 0x13) { // Pause / Break
                    SwitchToPC();
                    return (IntPtr)1;
                }

                // Check Hotkeys
                if (CheckHotkeyDown(_config.HotkeySwitch, kbd.vkCode)) {
                    ToggleMode();
                    return (IntPtr)1;
                }
                if (CheckHotkeyDown(_config.HotkeyReturn, kbd.vkCode)) {
                    SwitchToPC();
                    return (IntPtr)1;
                }
                if (CheckHotkeyDown(_config.HotkeyDisconnect, kbd.vkCode)) {
                    if (OnHotkeyTriggered != null) OnHotkeyTriggered("DISCONNECT");
                    return (IntPtr)1;
                }
                if (CheckHotkeyDown(_config.HotkeySlot1, kbd.vkCode)) {
                    if (OnHotkeyTriggered != null) OnHotkeyTriggered("SLOT_1");
                    return (IntPtr)1;
                }
                if (CheckHotkeyDown(_config.HotkeySlot2, kbd.vkCode)) {
                    if (OnHotkeyTriggered != null) OnHotkeyTriggered("SLOT_2");
                    return (IntPtr)1;
                }
                if (CheckHotkeyDown(_config.HotkeySlot3, kbd.vkCode)) {
                    if (OnHotkeyTriggered != null) OnHotkeyTriggered("SLOT_3");
                    return (IntPtr)1;
                }
                if (CheckHotkeyDown(_config.HotkeySlot4, kbd.vkCode)) {
                    if (OnHotkeyTriggered != null) OnHotkeyTriggered("SLOT_4");
                    return (IntPtr)1;
                }
                if (CheckHotkeyDown(_config.HotkeyPaste, kbd.vkCode)) {
                    if (OnHotkeyTriggered != null) OnHotkeyTriggered("PASTE");
                    return (IntPtr)1;
                }

                if (_isMobileActive) {
                    byte mod = 0;
                    if ((GetAsyncKeyState(0x11) & 0x8000) != 0) mod |= 0x01; // Ctrl
                    if ((GetAsyncKeyState(0x10) & 0x8000) != 0) mod |= 0x02; // Shift
                    if ((GetAsyncKeyState(0x12) & 0x8000) != 0) mod |= 0x04; // Alt
                    if ((GetAsyncKeyState(0x5B) & 0x8000) != 0) mod |= 0x08; // Win

                    byte hidCode = ConvertVkToHid((byte)kbd.vkCode);
                    if (hidCode != 0) {
                        _hardware.SendKeyboardReport(mod, new byte[] { hidCode });
                    }
                    return (IntPtr)1;
                }
            } else if (nCode >= 0 && (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)) {
                if (_isMobileActive) {
                    _hardware.SendKeyboardReport(0, new byte[] { 0 });
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode >= 0) {
                MSLLHOOKSTRUCT ms = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                // Ignore synthetic mouse events injected by SetCursorPos / SendInput
                if ((ms.flags & LLMHF_INJECTED) != 0 && wParam == (IntPtr)WM_MOUSEMOVE) {
                    return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
                }

                if (!_isMobileActive) {
                    if (_config.EdgeTraversalEnabled && _hardware.IsConnected && wParam == (IntPtr)WM_MOUSEMOVE) {
                        // Use Win32 GetSystemMetrics (physical pixels) to match WH_MOUSE_LL coordinates under DPI scaling
                        int vLeft = GetSystemMetrics(SM_XVIRTUALSCREEN);
                        int vTop = GetSystemMetrics(SM_YVIRTUALSCREEN);
                        int vWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
                        int vHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

                        int screenLeft = vLeft + 1;
                        int screenTop = vTop + 1;
                        int screenRight = vLeft + vWidth - 2;
                        int screenBottom = vTop + vHeight - 2;

                        bool crossEdge = false;
                        if (_config.PhoneEdge == "Right" && ms.pt.x >= screenRight) crossEdge = true;
                        else if (_config.PhoneEdge == "Left" && ms.pt.x <= screenLeft) crossEdge = true;
                        else if (_config.PhoneEdge == "Top" && ms.pt.y <= screenTop) crossEdge = true;
                        else if (_config.PhoneEdge == "Bottom" && ms.pt.y >= screenBottom) crossEdge = true;

                        if (crossEdge) {
                            SwitchToMobile();
                            return (IntPtr)1;
                        }
                    }
                } else {
                    if (_config.EdgeBounceReturnEnabled && wParam == (IntPtr)WM_MOUSEMOVE) {
                        int dx = ms.pt.x - _lastMousePos.x;
                        int dy = ms.pt.y - _lastMousePos.y;

                        bool returnBounce = false;
                        if (_config.PhoneEdge == "Right" && dx < -35) returnBounce = true;
                        else if (_config.PhoneEdge == "Left" && dx > 35) returnBounce = true;
                        else if (_config.PhoneEdge == "Top" && dy > 35) returnBounce = true;
                        else if (_config.PhoneEdge == "Bottom" && dy < -35) returnBounce = true;

                        if (returnBounce) {
                            SwitchToPC();
                            return (IntPtr)1;
                        }
                    }

                    if (wParam == (IntPtr)WM_MOUSEMOVE) {
                        int rawDx = (int)((ms.pt.x - _lastMousePos.x) * _config.MouseSpeed);
                        int rawDy = (int)((ms.pt.y - _lastMousePos.y) * _config.MouseSpeed);

                        sbyte sDx = (sbyte)Math.Max(-127, Math.Min(127, rawDx));
                        sbyte sDy = (sbyte)Math.Max(-127, Math.Min(127, rawDy));

                        _hardware.SendMouseReport(0, sDx, sDy, 0);
                        SetCursorPos(_lastMousePos.x, _lastMousePos.y);
                        return (IntPtr)1;
                    } else if (wParam == (IntPtr)WM_LBUTTONDOWN) {
                        _hardware.SendMouseReport(0x01, 0, 0, 0);
                        return (IntPtr)1;
                    } else if (wParam == (IntPtr)WM_LBUTTONUP) {
                        _hardware.SendMouseReport(0x00, 0, 0, 0);
                        return (IntPtr)1;
                    } else if (wParam == (IntPtr)WM_RBUTTONDOWN) {
                        _hardware.SendMouseReport(0x02, 0, 0, 0);
                        return (IntPtr)1;
                    } else if (wParam == (IntPtr)WM_RBUTTONUP) {
                        _hardware.SendMouseReport(0x00, 0, 0, 0);
                        return (IntPtr)1;
                    } else if (wParam == (IntPtr)WM_MOUSEWHEEL) {
                        short wheelDelta = (short)((ms.mouseData >> 16) & 0xFFFF);
                        sbyte w = (sbyte)(wheelDelta > 0 ? 1 : -1);
                        _hardware.SendMouseReport(0, 0, 0, w);
                        return (IntPtr)1;
                    }
                }
            }

            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private byte ConvertVkToHid(byte vk) {
            if (vk >= 0x41 && vk <= 0x5A) return (byte)(0x04 + (vk - 0x41)); // A-Z
            if (vk >= 0x31 && vk <= 0x39) return (byte)(0x1E + (vk - 0x31)); // 1-9
            if (vk == 0x30) return 0x27; // 0
            if (vk == 0x0D) return 0x28; // Enter
            if (vk == 0x1B) return 0x29; // Escape
            if (vk == 0x08) return 0x2A; // Backspace
            if (vk == 0x09) return 0x2B; // Tab
            if (vk == 0x20) return 0x2C; // Space
            if (vk >= 0x70 && vk <= 0x7B) return (byte)(0x3A + (vk - 0x70)); // F1-F12
            if (vk == 0x27) return 0x4F; // Right
            if (vk == 0x25) return 0x50; // Left
            if (vk == 0x28) return 0x51; // Down
            if (vk == 0x26) return 0x52; // Up
            if (vk == 0x15) return 0x90; // Hangul toggle
            return 0x00;
        }

        public void Uninstall() {
            if (_keyboardHook != IntPtr.Zero) {
                UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }
            if (_mouseHook != IntPtr.Zero) {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
        }

        public void Dispose() {
            Uninstall();
        }
    }

    // =========================================================================
    // MAIN APPLICATION WINDOW
    // =========================================================================
    public class MainWindow : Window {
        private static readonly Color BgDark = Color.FromRgb(24, 24, 28);
        private static readonly Color CardBg = Color.FromRgb(36, 36, 44);
        private static readonly Color CardBgHover = Color.FromRgb(48, 48, 58);
        private static readonly Color AccentBlue = Color.FromRgb(0, 140, 255);
        private static readonly Color AccentCyan = Color.FromRgb(0, 220, 255);
        private static readonly Color TextPrimary = Color.FromRgb(245, 245, 250);
        private static readonly Color TextSecondary = Color.FromRgb(160, 160, 175);
        private static readonly Color SuccessGreen = Color.FromRgb(46, 204, 113);
        private static readonly Color WarningYellow = Color.FromRgb(241, 196, 15);
        private static readonly Color BorderColor = Color.FromRgb(55, 55, 68);

        private StudioConfig _config;
        private EasyBlueHardware _hardware;
        private EasyBlueInputHook _inputHook;
        private DispatcherTimer _pollTimer;

        private Border _statusBadge;
        private TextBlock _statusBadgeText;
        private ContentControl _mainContentArea;
        private Button[] _navButtons;
        private int _currentTabIndex = 0;

        private Window _mobileIndicatorWindow;
        private System.Windows.Forms.NotifyIcon _trayIcon;
        private System.Windows.Forms.ContextMenuStrip _trayMenu;
        private bool _isExplicitExit = false;

        private Canvas _layoutCanvas;
        private Border _phoneVisualCard;
        private TextBlock _phonePosLabel;

        public MainWindow() {
            _config = StudioConfig.Load();
            _hardware = new EasyBlueHardware();
            _inputHook = new EasyBlueInputHook(_config, _hardware);

            _inputHook.OnModeChanged += (isMobile) => {
                Dispatcher.Invoke(new Action(() => {
                    UpdateMobileModeUI(isMobile);
                }));
            };

            _inputHook.OnHotkeyTriggered += (action) => {
                Dispatcher.Invoke(new Action(() => {
                    HandleHotkeyAction(action);
                }));
            };

            InitializeWindow();
            BuildModernUI();
            InitializeTray();
            InitializeMobileIndicator();

            _hardware.Connect();
            _inputHook.Install();

            _pollTimer = new DispatcherTimer();
            _pollTimer.Interval = TimeSpan.FromSeconds(3);
            _pollTimer.Tick += (s, e) => {
                if (!_hardware.IsConnected) _hardware.Connect();
                UpdateStatusBadge();
            };
            _pollTimer.Start();
            UpdateStatusBadge();

            // Intercept window close to keep running in system tray
            Closing += (s, e) => {
                if (!_isExplicitExit) {
                    e.Cancel = true;
                    Hide();
                    ShowTip("EasyBlue Studio 백그라운드 실행", "프로그램이 시스템 트레이에서 계속 실행 중입니다.\n트레이 아이콘을 클릭하여 언제든지 다시 열 수 있습니다.");
                }
            };

            StateChanged += (s, e) => {
                if (WindowState == WindowState.Minimized) {
                    Hide();
                    ShowTip("시스템 트레이 최소화", "트레이 아이콘을 클릭하면 창이 다시 복원됩니다.");
                }
            };
        }

        private void InitializeWindow() {
            Title = "EasyBlue Studio 2026";
            Width = 1040;
            Height = 690;
            MinWidth = 900;
            MinHeight = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(BgDark);
            Foreground = new SolidColorBrush(TextPrimary);
            FontFamily = new FontFamily("Malgun Gothic, Segoe UI");
            WindowStyle = WindowStyle.None;
            AllowsTransparency = false;
        }

        private void InitializeMobileIndicator() {
            _mobileIndicatorWindow = new Window {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                Width = 460,
                Height = 44,
                Left = (SystemParameters.PrimaryScreenWidth - 460) / 2,
                Top = 6
            };

            var border = new Border {
                Background = new SolidColorBrush(Color.FromArgb(235, 20, 20, 26)),
                BorderBrush = new SolidColorBrush(AccentCyan),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(22),
                Padding = new Thickness(16, 6, 16, 6),
                Effect = new DropShadowEffect { BlurRadius = 16, Color = AccentCyan, Opacity = 0.6 }
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var statusTxt = new TextBlock {
                Text = "📱 [모바일 제어 중] 복귀: " + _config.HotkeyReturn + " 또는 ESC",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(statusTxt, 0);
            grid.Children.Add(statusTxt);

            var btnReturn = new Button {
                Content = "🖥️ PC로 복귀",
                Background = new SolidColorBrush(AccentBlue),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Padding = new Thickness(10, 4, 10, 4),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnReturn.Click += (s, e) => _inputHook.SwitchToPC();
            Grid.SetColumn(btnReturn, 1);
            grid.Children.Add(btnReturn);

            border.Child = grid;
            _mobileIndicatorWindow.Content = border;
        }

        private void UpdateMobileModeUI(bool isMobile) {
            if (isMobile) {
                if (_mobileIndicatorWindow != null) _mobileIndicatorWindow.Show();
                if (_trayIcon != null) _trayIcon.Text = "EasyBlue Studio (📱 모바일 제어 중)";
            } else {
                if (_mobileIndicatorWindow != null) _mobileIndicatorWindow.Hide();
                if (_trayIcon != null) _trayIcon.Text = "EasyBlue Studio (🖥️ PC 제어 중)";
            }
            RefreshTrayMenuItems();
        }

        private void HandleHotkeyAction(string action) {
            if (action == "DISCONNECT") {
                _hardware.DisconnectDevice();
                ShowTip("연결 끊기", "블루투스 연결을 해제했습니다.");
            } else if (action.StartsWith("SLOT_")) {
                int slot = int.Parse(action.Substring(5)) - 1;
                _hardware.SelectDeviceSlot(slot);
                _config.ActiveSlotIndex = slot;
                _config.Save();
                ShowTip("슬롯 전환", "디바이스 슬롯 " + (slot + 1) + " (" + _config.DeviceSlots[slot] + ") 번으로 전환했습니다.");
            } else if (action == "PASTE") {
                StreamClipboardToMobile();
            }
        }

        private void BuildModernUI() {
            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var titleBar = CreateTitleBar();
            Grid.SetRow(titleBar, 0);
            rootGrid.Children.Add(titleBar);

            var bodyGrid = new Grid();
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var sidebar = CreateSidebar();
            Grid.SetColumn(sidebar, 0);
            bodyGrid.Children.Add(sidebar);

            _mainContentArea = new ContentControl {
                Margin = new Thickness(16, 0, 16, 16)
            };
            Grid.SetColumn(_mainContentArea, 1);
            bodyGrid.Children.Add(_mainContentArea);

            Grid.SetRow(bodyGrid, 1);
            rootGrid.Children.Add(bodyGrid);

            Content = rootGrid;
            SwitchTab(0);
        }

        private UIElement CreateTitleBar() {
            var bar = new Border {
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 24)),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 0, 12, 0)
            };

            bar.MouseDown += (s, e) => {
                if (e.ChangedButton == MouseButton.Left) DragMove();
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var logo = new TextBlock {
                Text = "⚡ EasyBlue",
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                Foreground = new SolidColorBrush(AccentCyan),
                VerticalAlignment = VerticalAlignment.Center
            };
            var subtitle = new TextBlock {
                Text = " Studio 2026",
                FontSize = 14,
                Foreground = new SolidColorBrush(TextPrimary),
                VerticalAlignment = VerticalAlignment.Center
            };
            titlePanel.Children.Add(logo);
            titlePanel.Children.Add(subtitle);
            Grid.SetColumn(titlePanel, 0);
            grid.Children.Add(titlePanel);

            _statusBadge = new Border {
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 4, 10, 4),
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 50)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _statusBadgeText = new TextBlock {
                Text = "🔍 동글 감지 중...",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(TextSecondary)
            };
            _statusBadge.Child = _statusBadgeText;
            Grid.SetColumn(_statusBadge, 1);
            grid.Children.Add(_statusBadge);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var btnMin = CreateIconBtn("—", () => WindowState = WindowState.Minimized);
            var btnClose = CreateIconBtn("✕", () => {
                _config.Save();
                Hide();
                ShowTip("EasyBlue Studio 백그라운드 실행", "프로그램이 시스템 트레이에서 계속 실행 중입니다.");
            });
            btnPanel.Children.Add(btnMin);
            btnPanel.Children.Add(btnClose);
            Grid.SetColumn(btnPanel, 2);
            grid.Children.Add(btnPanel);

            bar.Child = grid;
            return bar;
        }

        private Button CreateIconBtn(string icon, Action onClick) {
            var btn = new Button {
                Content = icon,
                Width = 36,
                Height = 32,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(TextSecondary),
                BorderThickness = new Thickness(0),
                FontSize = 13,
                Cursor = Cursors.Hand
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private UIElement CreateSidebar() {
            var border = new Border {
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 34)),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(8, 16, 8, 16)
            };

            var stack = new StackPanel();

            string[] tabNames = new string[] {
                "🖥️ 화면 배치 & 플로우",
                "📶 페어링 & 장치 관리",
                "⌨️ 단축키 설정",
                "⚙️ 입력 & 환경 설정",
                "🎮 모바일 리모컨 덱",
                "🛡️ 보안 PC & 진단"
            };

            _navButtons = new Button[tabNames.Length];
            for (int i = 0; i < tabNames.Length; i++) {
                int idx = i;
                var btn = new Button {
                    Content = tabNames[i],
                    Height = 44,
                    Margin = new Thickness(0, 0, 0, 6),
                    Padding = new Thickness(14, 0, 14, 0),
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush(i == 0 ? AccentCyan : TextSecondary),
                    FontWeight = i == 0 ? FontWeights.Bold : FontWeights.Normal,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                btn.Click += (s, e) => SwitchTab(idx);
                _navButtons[i] = btn;
                stack.Children.Add(btn);
            }

            var toggleCard = new Border {
                Margin = new Thickness(0, 24, 0, 0),
                Background = new SolidColorBrush(CardBg),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12)
            };
            var toggleStack = new StackPanel();
            toggleStack.Children.Add(new TextBlock {
                Text = "빠른 입력 전환",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(TextSecondary),
                Margin = new Thickness(0, 0, 0, 8)
            });

            var btnQuickSwitch = new Button {
                Content = "🔄 PC ↔ 모바일 전환",
                Height = 36,
                Background = new SolidColorBrush(AccentBlue),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnQuickSwitch.Click += (s, e) => _inputHook.ToggleMode();
            toggleStack.Children.Add(btnQuickSwitch);
            toggleCard.Child = toggleStack;
            stack.Children.Add(toggleCard);

            border.Child = stack;
            return border;
        }

        private void SwitchTab(int index) {
            _currentTabIndex = index;
            for (int i = 0; i < _navButtons.Length; i++) {
                bool active = (i == index);
                _navButtons[i].Foreground = new SolidColorBrush(active ? AccentCyan : TextSecondary);
                _navButtons[i].FontWeight = active ? FontWeights.Bold : FontWeights.Normal;
                _navButtons[i].Background = active ? new SolidColorBrush(Color.FromRgb(40, 42, 54)) : Brushes.Transparent;
            }

            switch (index) {
                case 0: _mainContentArea.Content = BuildLayoutTab(); break;
                case 1: _mainContentArea.Content = BuildPairingTab(); break;
                case 2: _mainContentArea.Content = BuildHotkeysTab(); break;
                case 3: _mainContentArea.Content = BuildPreferencesTab(); break;
                case 4: _mainContentArea.Content = BuildRemoteDeckTab(); break;
                case 5: _mainContentArea.Content = BuildDiagnosticsTab(); break;
            }
        }

        // =========================================================================
        // TAB 1: LAYOUT & UNIVERSAL FLOW CANVAS
        // =========================================================================
        private UIElement BuildLayoutTab() {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel { Margin = new Thickness(8, 16, 8, 12) };
            header.Children.Add(new TextBlock {
                Text = "🖥️ 다중 모니터 & 스마트폰 공간 배치",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            header.Children.Add(new TextBlock {
                Text = "스마트폰을 원하는 위치에 배치하세요. 마우스가 해당 모니터 가장자리를 넘어가면 자동으로 스마트폰으로 전환됩니다.",
                FontSize = 12.5,
                Foreground = new SolidColorBrush(TextSecondary),
                Margin = new Thickness(0, 4, 0, 0)
            });
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var canvasBorder = new Border {
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                ClipToBounds = true,
                Margin = new Thickness(8, 0, 8, 12)
            };
            _layoutCanvas = new Canvas();
            canvasBorder.Child = _layoutCanvas;
            Grid.SetRow(canvasBorder, 1);
            root.Children.Add(canvasBorder);

            _layoutCanvas.Loaded += (s, e) => RenderLayoutCanvas();

            var bottomBar = new Border {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 38)),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(8, 0, 8, 8)
            };
            var bottomGrid = new Grid();
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _phonePosLabel = new TextBlock {
                Text = "현재 모바일 위치: " + _config.PhoneEdge + " (마우스 경계 자동 전환: " + (_config.EdgeTraversalEnabled ? "켜짐" : "꺼짐") + ")",
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };
            Grid.SetColumn(_phonePosLabel, 0);
            bottomGrid.Children.Add(_phonePosLabel);

            var edgeBtnRow = new StackPanel { Orientation = Orientation.Horizontal };
            string[] edges = new string[] { "Left", "Top", "Right", "Bottom" };
            string[] edgeLabels = new string[] { "◀ 좌측", "▲ 상단", "우측 ▶", "▼ 하단" };
            for (int i = 0; i < edges.Length; i++) {
                string edge = edges[i];
                var btn = new Button {
                    Content = edgeLabels[i],
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(6, 0, 0, 0),
                    Background = _config.PhoneEdge == edge ? new SolidColorBrush(AccentBlue) : new SolidColorBrush(CardBgHover),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                btn.Click += (s, e) => {
                    _config.PhoneEdge = edge;
                    _config.Save();
                    RenderLayoutCanvas();
                    _phonePosLabel.Text = "현재 모바일 위치: " + _config.PhoneEdge + " (마우스 경계 자동 전환: " + (_config.EdgeTraversalEnabled ? "켜짐" : "꺼짐") + ")";
                };
                edgeBtnRow.Children.Add(btn);
            }
            Grid.SetColumn(edgeBtnRow, 1);
            bottomGrid.Children.Add(edgeBtnRow);

            bottomBar.Child = bottomGrid;
            Grid.SetRow(bottomBar, 2);
            root.Children.Add(bottomBar);

            return root;
        }

        private void RenderLayoutCanvas() {
            if (_layoutCanvas == null || _layoutCanvas.ActualWidth < 10) return;
            _layoutCanvas.Children.Clear();

            double cw = _layoutCanvas.ActualWidth;
            double ch = _layoutCanvas.ActualHeight;

            double pcW = 260, pcH = 160;
            double pcX = (cw - pcW) / 2;
            double pcY = (ch - pcH) / 2;

            var pcCard = new Border {
                Width = pcW,
                Height = pcH,
                Background = new SolidColorBrush(Color.FromRgb(48, 52, 65)),
                BorderBrush = new SolidColorBrush(AccentBlue),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(10),
                Effect = new DropShadowEffect { BlurRadius = 14, Color = Colors.Black, Opacity = 0.5 }
            };
            var pcStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            pcStack.Children.Add(new TextBlock { Text = "🖥️ 메인 PC 모니터", FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, FontSize = 14 });
            pcStack.Children.Add(new TextBlock { Text = string.Format("{0} x {1}", (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight), Foreground = new SolidColorBrush(TextSecondary), HorizontalAlignment = HorizontalAlignment.Center, FontSize = 11, Margin = new Thickness(0, 4, 0, 0) });
            pcCard.Child = pcStack;

            Canvas.SetLeft(pcCard, pcX);
            Canvas.SetTop(pcCard, pcY);
            _layoutCanvas.Children.Add(pcCard);

            double phW = 110, phH = 170;
            double phX = pcX + pcW + 36, phY = pcY - 5;
            if (_config.PhoneEdge == "Left") phX = pcX - phW - 36;
            else if (_config.PhoneEdge == "Top") { phX = pcX + (pcW - phW) / 2; phY = pcY - phH - 24; }
            else if (_config.PhoneEdge == "Bottom") { phX = pcX + (pcW - phW) / 2; phY = pcY + pcH + 24; }

            _phoneVisualCard = new Border {
                Width = phW,
                Height = phH,
                Background = new SolidColorBrush(Color.FromRgb(22, 24, 30)),
                BorderBrush = new SolidColorBrush(AccentCyan),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(18),
                Effect = new DropShadowEffect { BlurRadius = 16, Color = AccentCyan, Opacity = 0.4 }
            };
            var phStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            phStack.Children.Add(new TextBlock { Text = "📱", FontSize = 28, HorizontalAlignment = HorizontalAlignment.Center });
            phStack.Children.Add(new TextBlock { Text = _config.DeviceSlots[_config.ActiveSlotIndex], FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, FontSize = 11, Margin = new Thickness(0, 6, 0, 0) });
            phStack.Children.Add(new TextBlock { Text = "Bluetooth Connected", Foreground = new SolidColorBrush(SuccessGreen), HorizontalAlignment = HorizontalAlignment.Center, FontSize = 9 });
            _phoneVisualCard.Child = phStack;

            Canvas.SetLeft(_phoneVisualCard, phX);
            Canvas.SetTop(_phoneVisualCard, phY);
            _layoutCanvas.Children.Add(_phoneVisualCard);
        }

        // =========================================================================
        // TAB 2: BLUETOOTH PAIRING & DEVICE MANAGEMENT
        // =========================================================================
        private UIElement BuildPairingTab() {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(8, 16, 8, 16) };

            var header = new TextBlock { Text = "📶 블루투스 페어링 및 디바이스 슬롯 관리", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            var desc = new TextBlock { Text = "새로운 스마트폰을 페어링하거나, 최대 4개의 등록된 기기 슬롯을 직접 선택하고 관리합니다.", FontSize = 12.5, Foreground = new SolidColorBrush(TextSecondary), Margin = new Thickness(0, 4, 0, 16) };
            stack.Children.Add(header);
            stack.Children.Add(desc);

            var pairCard = new Border {
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(AccentCyan),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18),
                Margin = new Thickness(0, 0, 0, 16)
            };
            var pairGrid = new Grid();
            pairGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pairGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pairInfo = new StackPanel();
            pairInfo.Children.Add(new TextBlock { Text = "새 디바이스 페어링 모드 (Pairing Standby)", FontWeight = FontWeights.Bold, FontSize = 14, Foreground = Brushes.White });
            pairInfo.Children.Add(new TextBlock {
                Text = "동글을 블루투스 검색 가능 상태로 전환합니다. 스마트폰의 블루투스 설정에서 'EasyBlue'를 검색하여 연결하세요.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(TextSecondary),
                Margin = new Thickness(0, 4, 0, 0)
            });
            Grid.SetColumn(pairInfo, 0);
            pairGrid.Children.Add(pairInfo);

            var pairBtn = new Button {
                Content = "⚡ 페어링 대기 모드 시작",
                Height = 40,
                Padding = new Thickness(18, 0, 18, 0),
                Background = new SolidColorBrush(AccentBlue),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            pairBtn.Click += (s, e) => {
                bool ok = _hardware.EnterPairingMode();
                if (ok) {
                    ShowPairingPinDialog();
                } else {
                    MessageBox.Show("동글과 통신할 수 없습니다. USB 연결 상태를 확인하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };
            Grid.SetColumn(pairBtn, 1);
            pairGrid.Children.Add(pairBtn);

            pairCard.Child = pairGrid;
            stack.Children.Add(pairCard);

            var slotsHeader = new TextBlock { Text = "등록된 디바이스 슬롯 (최대 4대 멀티 페어링)", FontWeight = FontWeights.Bold, FontSize = 14, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 8) };
            stack.Children.Add(slotsHeader);

            for (int i = 0; i < 4; i++) {
                int slotIdx = i;
                bool isActive = (_config.ActiveSlotIndex == slotIdx);

                var slotCard = new Border {
                    Background = new SolidColorBrush(isActive ? Color.FromRgb(40, 48, 64) : CardBg),
                    BorderBrush = new SolidColorBrush(isActive ? AccentCyan : BorderColor),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var slotGrid = new Grid();
                slotGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
                slotGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                slotGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var iconTxt = new TextBlock {
                    Text = (slotIdx == 0 ? "📱" : (slotIdx == 1 ? "📟" : "📱")),
                    FontSize = 24,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(iconTxt, 0);
                slotGrid.Children.Add(iconTxt);

                var slotInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
                nameRow.Children.Add(new TextBlock {
                    Text = "슬롯 " + (slotIdx + 1) + ": ",
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(AccentCyan),
                    FontSize = 13
                });
                var txtName = new TextBox {
                    Text = _config.DeviceSlots[slotIdx],
                    Width = 200,
                    Height = 26,
                    Background = new SolidColorBrush(Color.FromRgb(24, 24, 30)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(BorderColor),
                    Padding = new Thickness(4, 2, 4, 2)
                };
                txtName.LostFocus += (s, e) => {
                    _config.DeviceSlots[slotIdx] = txtName.Text.Trim();
                    _config.Save();
                    RefreshTrayMenuItems();
                };
                nameRow.Children.Add(txtName);
                slotInfo.Children.Add(nameRow);

                slotInfo.Children.Add(new TextBlock {
                    Text = isActive ? "● 현재 연결된 활성 장치" : "○ 대기 중 (선택 시 바로 전환)",
                    FontSize = 11,
                    Foreground = isActive ? new SolidColorBrush(SuccessGreen) : new SolidColorBrush(TextSecondary),
                    Margin = new Thickness(0, 4, 0, 0)
                });
                Grid.SetColumn(slotInfo, 1);
                slotGrid.Children.Add(slotInfo);

                var actionRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                var btnSelect = new Button {
                    Content = isActive ? "✓ 연결 중" : "전환하기",
                    Padding = new Thickness(14, 6, 14, 6),
                    Background = isActive ? new SolidColorBrush(SuccessGreen) : new SolidColorBrush(AccentBlue),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                btnSelect.Click += (s, e) => {
                    _hardware.SelectDeviceSlot(slotIdx);
                    _config.ActiveSlotIndex = slotIdx;
                    _config.Save();
                    SwitchTab(1);
                    RefreshTrayMenuItems();
                    ShowTip("디바이스 전환", "슬롯 " + (slotIdx + 1) + "번 장치로 전환 명령을 전송했습니다.");
                };
                actionRow.Children.Add(btnSelect);

                var btnDisconnect = new Button {
                    Content = "연결 해제",
                    Margin = new Thickness(8, 0, 0, 0),
                    Padding = new Thickness(10, 6, 10, 6),
                    Background = new SolidColorBrush(CardBgHover),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(BorderColor),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand
                };
                btnDisconnect.Click += (s, e) => {
                    _hardware.DisconnectDevice();
                    ShowTip("연결 해제", "슬롯 " + (slotIdx + 1) + " 연결을 해제했습니다.");
                };
                actionRow.Children.Add(btnDisconnect);

                Grid.SetColumn(actionRow, 2);
                slotGrid.Children.Add(actionRow);

                slotCard.Child = slotGrid;
                stack.Children.Add(slotCard);
            }

            var resetCard = new Border {
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 12, 0, 0)
            };
            var resetRow = new DockPanel();
            var resetTxt = new StackPanel();
            resetTxt.Children.Add(new TextBlock { Text = "페어링 정보 전체 초기화", FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            resetTxt.Children.Add(new TextBlock { Text = "동글 메모리에 저장된 4개의 모든 기기 페어링 목록을 완전히 삭제합니다.", FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary) });
            resetRow.Children.Add(resetTxt);

            var btnReset = new Button {
                Content = "⚠️ 전체 페어링 삭제",
                Padding = new Thickness(14, 6, 14, 6),
                Background = new SolidColorBrush(Color.FromRgb(160, 40, 40)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnReset.Click += (s, e) => {
                if (MessageBox.Show("정말로 모든 페어링 목록을 삭제하시겠습니까?", "페어링 초기화", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) {
                    _hardware.ClearPairings();
                    ShowTip("초기화 완료", "모든 페어링 정보를 삭제했습니다.");
                }
            };
            DockPanel.SetDock(btnReset, Dock.Right);
            resetRow.Children.Add(btnReset);
            resetCard.Child = resetRow;
            stack.Children.Add(resetCard);

            scroll.Content = stack;
            return scroll;
        }

        // =========================================================================
        // TAB 3: CUSTOMIZABLE HOTKEYS
        // =========================================================================
        private UIElement BuildHotkeysTab() {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(8, 16, 8, 16) };

            var header = new TextBlock { Text = "⌨️ 단축키 설정 및 커스터마이징", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            var desc = new TextBlock { Text = "PC와 모바일 간의 입력 전환, 복귀, 기기 직접 선택 단축키를 사용자의 취향에 맞춰 자유롭게 변경합니다.", FontSize = 12.5, Foreground = new SolidColorBrush(TextSecondary), Margin = new Thickness(0, 4, 0, 16) };
            stack.Children.Add(header);
            stack.Children.Add(desc);

            var card = new Border {
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18)
            };
            var cardStack = new StackPanel();

            AddHotkeyRow(cardStack, "입력 전환 단축키 (PC ↔ 모바일 토글)", "PC 제어 중 스마트폰으로 넘어가거나 다시 토글하는 주 단축키", _config.HotkeySwitch, (newKey) => {
                _config.HotkeySwitch = newKey;
                _config.Save();
                RefreshTrayMenuItems();
            });

            AddHotkeyRow(cardStack, "모바일에서 PC로 복귀 단축키", "모바일 제어 중 즉시 PC 제어 모드로 안전하게 복귀하는 단축키", _config.HotkeyReturn, (newKey) => {
                _config.HotkeyReturn = newKey;
                _config.Save();
                RefreshTrayMenuItems();
            });

            AddHotkeyRow(cardStack, "블루투스 연결 끊기 단축키", "진행 중인 블루투스 무선 연결을 즉시 종료하는 단축키", _config.HotkeyDisconnect, (newKey) => {
                _config.HotkeyDisconnect = newKey;
                _config.Save();
            });

            AddHotkeyRow(cardStack, "슬롯 1번 직접 선택 단축키", _config.DeviceSlots[0] + " 기기로 바로 전환", _config.HotkeySlot1, (newKey) => {
                _config.HotkeySlot1 = newKey;
                _config.Save();
            });

            AddHotkeyRow(cardStack, "슬롯 2번 직접 선택 단축키", _config.DeviceSlots[1] + " 기기로 바로 전환", _config.HotkeySlot2, (newKey) => {
                _config.HotkeySlot2 = newKey;
                _config.Save();
            });

            AddHotkeyRow(cardStack, "슬롯 3번 직접 선택 단축키", _config.DeviceSlots[2] + " 기기로 바로 전환", _config.HotkeySlot3, (newKey) => {
                _config.HotkeySlot3 = newKey;
                _config.Save();
            });

            AddHotkeyRow(cardStack, "슬롯 4번 직접 선택 단축키", _config.DeviceSlots[3] + " 기기로 바로 전환", _config.HotkeySlot4, (newKey) => {
                _config.HotkeySlot4 = newKey;
                _config.Save();
            });

            AddHotkeyRow(cardStack, "스마트 클립보드 붙여넣기 단축키", "PC 클립보드 텍스트를 모바일 스마트폰에 분해 없이 한글 조합으로 자동 타이핑", _config.HotkeyPaste, (newKey) => {
                _config.HotkeyPaste = newKey;
                _config.Save();
            });

            card.Child = cardStack;
            stack.Children.Add(card);

            scroll.Content = stack;
            return scroll;
        }

        private void AddHotkeyRow(StackPanel parent, string title, string subtitle, string currentValue, Action<string> onUpdate) {
            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };

            var textStack = new StackPanel();
            textStack.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            textStack.Children.Add(new TextBlock { Text = subtitle, FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary) });
            row.Children.Add(textStack);

            var rightPanel = new StackPanel { Orientation = Orientation.Horizontal };
            DockPanel.SetDock(rightPanel, Dock.Right);

            var keyBadge = new Border {
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 26)),
                BorderBrush = new SolidColorBrush(AccentBlue),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(0, 0, 8, 0)
            };
            var keyText = new TextBlock {
                Text = currentValue,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(AccentCyan),
                FontSize = 12
            };
            keyBadge.Child = keyText;
            rightPanel.Children.Add(keyBadge);

            var btnChange = new Button {
                Content = "단축키 변경",
                Padding = new Thickness(12, 4, 12, 4),
                Background = new SolidColorBrush(CardBgHover),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            btnChange.Click += (s, e) => {
                ShowHotkeyRecorder(title, (capturedKey) => {
                    keyText.Text = capturedKey;
                    onUpdate(capturedKey);
                });
            };
            rightPanel.Children.Add(btnChange);

            row.Children.Add(rightPanel);
            parent.Children.Add(row);
        }

        private void ShowHotkeyRecorder(string title, Action<string> onRecorded) {
            var dlg = new Window {
                Title = "단축키 녹음",
                Width = 380,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(BgDark),
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize
            };

            var stack = new StackPanel { Margin = new Thickness(20), VerticalAlignment = VerticalAlignment.Center };
            stack.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.Bold, FontSize = 14, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center });
            stack.Children.Add(new TextBlock { Text = "원하는 키 조합을 직접 누르세요 (예: Ctrl+Alt+화살표, F12 등)", FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary), Margin = new Thickness(0, 6, 0, 16), HorizontalAlignment = HorizontalAlignment.Center });

            var keyDisplay = new Border {
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(AccentCyan),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 10, 16, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var keyDisplayText = new TextBlock { Text = "키를 누르세요...", FontWeight = FontWeights.Bold, FontSize = 14, Foreground = new SolidColorBrush(AccentCyan) };
            keyDisplay.Child = keyDisplayText;
            stack.Children.Add(keyDisplay);

            dlg.KeyDown += (s, e) => {
                e.Handled = true;
                var sb = new StringBuilder();
                if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) sb.Append("Ctrl+");
                if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) sb.Append("Alt+");
                if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) sb.Append("Shift+");
                if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) sb.Append("Win+");

                Key k = e.Key == Key.System ? e.SystemKey : e.Key;
                if (k != Key.LeftCtrl && k != Key.RightCtrl && k != Key.LeftAlt && k != Key.RightAlt &&
                    k != Key.LeftShift && k != Key.RightShift && k != Key.LWin && k != Key.RWin) {
                    sb.Append(k.ToString());
                    string result = sb.ToString();
                    keyDisplayText.Text = result;
                    onRecorded(result);
                    dlg.DialogResult = true;
                }
            };

            dlg.Content = stack;
            dlg.ShowDialog();
        }

        // =========================================================================
        // TAB 4: PREFERENCES & INPUT BEHAVIOR
        // =========================================================================
        private UIElement BuildPreferencesTab() {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(8, 16, 8, 16) };

            var header = new TextBlock { Text = "⚙️ 입력 및 하드웨어 환경설정", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            var desc = new TextBlock { Text = "마우스 감도, 경계 이동 조건, 동글 LED 및 클립보드 스트리머 설정을 구성합니다.", FontSize = 12.5, Foreground = new SolidColorBrush(TextSecondary), Margin = new Thickness(0, 4, 0, 16) };
            stack.Children.Add(header);
            stack.Children.Add(desc);

            var card = new Border {
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18)
            };
            var cardStack = new StackPanel();

            var s1 = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
            var s1Txt = new StackPanel();
            s1Txt.Children.Add(new TextBlock { Text = "모바일 마우스 속도 배율", FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            s1Txt.Children.Add(new TextBlock { Text = "모바일 제어 시 마우스 커서의 이동 속도를 조절합니다 (0.5x ~ 3.0x)", FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary) });
            s1.Children.Add(s1Txt);

            var s1Val = new TextBlock { Text = _config.MouseSpeed.ToString("0.0") + "x", Foreground = new SolidColorBrush(AccentCyan), FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            DockPanel.SetDock(s1Val, Dock.Right);
            s1.Children.Add(s1Val);

            var slider = new Slider {
                Minimum = 0.5,
                Maximum = 3.0,
                Value = _config.MouseSpeed,
                Width = 140,
                VerticalAlignment = VerticalAlignment.Center
            };
            slider.ValueChanged += (s, e) => {
                _config.MouseSpeed = slider.Value;
                s1Val.Text = slider.Value.ToString("0.0") + "x";
                _config.Save();
            };
            DockPanel.SetDock(slider, Dock.Right);
            s1.Children.Add(slider);
            cardStack.Children.Add(s1);

            var s2 = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
            var s2Txt = new StackPanel();
            s2Txt.Children.Add(new TextBlock { Text = "마우스 화면 경계 이동 활성화 (Flow)", FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            s2Txt.Children.Add(new TextBlock { Text = "마우스가 지정된 화면 모서리에 닿았을 때 자동으로 스마트폰으로 넘어갑니다.", FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary) });
            s2.Children.Add(s2Txt);

            var chkEdge = new CheckBox { IsChecked = _config.EdgeTraversalEnabled, VerticalAlignment = VerticalAlignment.Center };
            chkEdge.Checked += (s, e) => { _config.EdgeTraversalEnabled = true; _config.Save(); RefreshTrayMenuItems(); };
            chkEdge.Unchecked += (s, e) => { _config.EdgeTraversalEnabled = false; _config.Save(); RefreshTrayMenuItems(); };
            DockPanel.SetDock(chkEdge, Dock.Right);
            s2.Children.Add(chkEdge);
            cardStack.Children.Add(s2);

            var s3 = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
            var s3Txt = new StackPanel();
            s3Txt.Children.Add(new TextBlock { Text = "마우스 역방향 튕김으로 PC 복귀 (Bounce Return)", FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            s3Txt.Children.Add(new TextBlock { Text = "모바일 제어 중 마우스를 반대 방향 경계로 밀면 즉시 PC 제어로 자동 복귀합니다.", FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary) });
            s3.Children.Add(s3Txt);

            var chkBounce = new CheckBox { IsChecked = _config.EdgeBounceReturnEnabled, VerticalAlignment = VerticalAlignment.Center };
            chkBounce.Checked += (s, e) => { _config.EdgeBounceReturnEnabled = true; _config.Save(); };
            chkBounce.Unchecked += (s, e) => { _config.EdgeBounceReturnEnabled = false; _config.Save(); };
            DockPanel.SetDock(chkBounce, Dock.Right);
            s3.Children.Add(chkBounce);
            cardStack.Children.Add(s3);

            var s4 = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
            var s4Txt = new StackPanel();
            s4Txt.Children.Add(new TextBlock { Text = "이지블루 동글 LED 켜기", FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            s4Txt.Children.Add(new TextBlock { Text = "동글 본체의 상태 표시 파란색 LED 동작을 제어합니다.", FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary) });
            s4.Children.Add(s4Txt);

            var chkLed = new CheckBox { IsChecked = _config.LedEnabled, VerticalAlignment = VerticalAlignment.Center };
            chkLed.Checked += (s, e) => {
                _config.LedEnabled = true;
                _hardware.SetLed(true);
                _config.Save();
                RefreshTrayMenuItems();
            };
            chkLed.Unchecked += (s, e) => {
                _config.LedEnabled = false;
                _hardware.SetLed(false);
                _config.Save();
                RefreshTrayMenuItems();
            };
            DockPanel.SetDock(chkLed, Dock.Right);
            s4.Children.Add(chkLed);
            cardStack.Children.Add(s4);

            var s5 = new DockPanel();
            var s5Txt = new StackPanel();
            s5Txt.Children.Add(new TextBlock { Text = "동글 입력 프로필 모드", FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            s5Txt.Children.Add(new TextBlock { Text = "콤보 (키보드+마우스) 또는 전용 모드를 선택합니다.", FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary) });
            s5.Children.Add(s5Txt);

            var comboMode = new ComboBox { Width = 150, VerticalAlignment = VerticalAlignment.Center };
            comboMode.Items.Add("콤보 (키보드+마우스)");
            comboMode.Items.Add("키보드 전용");
            comboMode.Items.Add("마우스 전용");
            comboMode.SelectedIndex = _config.InputMode;
            comboMode.SelectionChanged += (s, e) => {
                _config.InputMode = comboMode.SelectedIndex;
                _hardware.SetInputMode(_config.InputMode);
                _config.Save();
            };
            DockPanel.SetDock(comboMode, Dock.Right);
            s5.Children.Add(comboMode);
            cardStack.Children.Add(s5);

            card.Child = cardStack;
            stack.Children.Add(card);

            scroll.Content = stack;
            return scroll;
        }

        // =========================================================================
        // TAB 5: MOBILE REMOTE DECK & CLIPBOARD STREAMER
        // =========================================================================
        private UIElement BuildRemoteDeckTab() {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(8, 16, 8, 16) };

            var header = new TextBlock { Text = "🎮 모바일 리모컨 덱 & 클립보드 스트리머", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            var desc = new TextBlock { Text = "스마트폰 원격 제어 단축키 및 한글 분해 없는 스마트 클립보드 전송기입니다.", FontSize = 12.5, Foreground = new SolidColorBrush(TextSecondary), Margin = new Thickness(0, 4, 0, 16) };
            stack.Children.Add(header);
            stack.Children.Add(desc);

            var deckCard = new Border {
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18),
                Margin = new Thickness(0, 0, 0, 16)
            };
            var deckStack = new StackPanel();
            deckStack.Children.Add(new TextBlock { Text = "스마트폰 시스템 제어", FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 12) });

            var row1 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            row1.Children.Add(CreateDeckBtn("🏠 홈 (Home)", () => _hardware.SendKeyboardReport(0x08, null)));
            row1.Children.Add(CreateDeckBtn("◀ 뒤로가기 (Back)", () => _hardware.SendKeyboardReport(0, new byte[] { 0x29 })));
            row1.Children.Add(CreateDeckBtn("📑 최근 앱 (Recents)", () => _hardware.SendKeyboardReport(0x04, new byte[] { 0x2B })));
            row1.Children.Add(CreateDeckBtn("🔒 화면 잠금/켜기", () => _hardware.SendKeyboardReport(0x08, new byte[] { 0x0F })));
            deckStack.Children.Add(row1);

            var row2 = new StackPanel { Orientation = Orientation.Horizontal };
            row2.Children.Add(CreateDeckBtn("🔊 볼륨 +", () => _hardware.SendKeyboardReport(0, new byte[] { 0x80 })));
            row2.Children.Add(CreateDeckBtn("🔉 볼륨 -", () => _hardware.SendKeyboardReport(0, new byte[] { 0x81 })));
            row2.Children.Add(CreateDeckBtn("🔇 음소거", () => _hardware.SendKeyboardReport(0, new byte[] { 0x7F })));
            row2.Children.Add(CreateDeckBtn("⏯ 재생 / 정지", () => _hardware.SendKeyboardReport(0, new byte[] { 0xCD })));
            deckStack.Children.Add(row2);

            deckCard.Child = deckStack;
            stack.Children.Add(deckCard);

            var textCard = new Border {
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18)
            };
            var textStack = new StackPanel();
            textStack.Children.Add(new TextBlock { Text = "스마트 텍스트 원격 주입 (한글 자모 분리 방지)", FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 8) });

            var inputTxt = new TextBox {
                Height = 80,
                Background = new SolidColorBrush(Color.FromRgb(24, 24, 30)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(BorderColor),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(8),
                Text = "이지블루 스튜디오를 통해 전송할 텍스트를 입력하세요."
            };
            textStack.Children.Add(inputTxt);

            var sendBtn = new Button {
                Content = "🚀 모바일로 텍스트 즉시 전송",
                Height = 36,
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(AccentBlue),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            sendBtn.Click += (s, e) => {
                Clipboard.SetText(inputTxt.Text);
                StreamClipboardToMobile();
            };
            textStack.Children.Add(sendBtn);

            textCard.Child = textStack;
            stack.Children.Add(textCard);

            scroll.Content = stack;
            return scroll;
        }

        private Button CreateDeckBtn(string text, Action action) {
            var btn = new Button {
                Content = text,
                Height = 36,
                Padding = new Thickness(14, 0, 14, 0),
                Margin = new Thickness(0, 0, 8, 0),
                Background = new SolidColorBrush(CardBgHover),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            btn.Click += (s, e) => action();
            return btn;
        }

        private void StreamClipboardToMobile() {
            try {
                _inputHook.SwitchToMobile();
                _hardware.SendKeyboardReport(0x01, new byte[] { 0x19 }); // Ctrl+V
                Thread.Sleep(50);
                _hardware.SendKeyboardReport(0, new byte[] { 0 });
                ShowTip("클립보드 주입", "스마트폰으로 클립보드 붙여넣기 신호를 전송했습니다.");
            } catch (Exception ex) {
                MessageBox.Show("클립보드 전송 오류: " + ex.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // =========================================================================
        // TAB 6: SECURITY PC & DIAGNOSTICS
        // =========================================================================
        private UIElement BuildDiagnosticsTab() {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(8, 16, 8, 16) };

            var header = new TextBlock { Text = "🛡️ 사내 보안 PC 및 하드웨어 진단", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            var desc = new TextBlock { Text = "사내 DLP(매체제어) 솔루션 차단 여부 및 순수 HID 보안 모드를 진단합니다.", FontSize = 12.5, Foreground = new SolidColorBrush(TextSecondary), Margin = new Thickness(0, 4, 0, 16) };
            stack.Children.Add(header);
            stack.Children.Add(desc);

            var hwCard = new Border {
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18)
            };
            var hwStack = new StackPanel();
            hwStack.Children.Add(new TextBlock { Text = "하드웨어 진단 상태", FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 8) });

            string secStatus = _hardware.IsConnected ? (_hardware.IsPureHidMode ? "✅ [보안PC 순수 HID 모드] 가상 드라이브 완전 차단됨 (사내 DLP 안전)" : "ℹ️ [일반 모드] 가상 드라이브(D:) 마운트됨") : "⚠️ 이지블루 동글이 연결되지 않았습니다.";
            var badge = new Border {
                Background = new SolidColorBrush(_hardware.IsPureHidMode ? Color.FromRgb(20, 60, 40) : Color.FromRgb(40, 40, 50)),
                BorderBrush = new SolidColorBrush(_hardware.IsPureHidMode ? SuccessGreen : BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 12)
            };
            badge.Child = new TextBlock { Text = secStatus, FontWeight = FontWeights.Bold, Foreground = _hardware.IsPureHidMode ? new SolidColorBrush(SuccessGreen) : Brushes.White };
            hwStack.Children.Add(badge);

            hwStack.Children.Add(new TextBlock { Text = "• 장치 ID: USB\\VID_1D54&PID_B002 (Zaram Smart BKM)", FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary) });
            hwStack.Children.Add(new TextBlock { Text = "• 통신 프로토콜: USB HID (UsagePage: 0xFFA0, Usage: 0x0001 - Driverless)", FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary), Margin = new Thickness(0, 2, 0, 0) });
            hwStack.Children.Add(new TextBlock { Text = "• 장치 경로: " + (_hardware.DevicePath ?? "미연결"), FontSize = 10, Foreground = new SolidColorBrush(TextSecondary), Margin = new Thickness(0, 2, 0, 0) });

            hwCard.Child = hwStack;
            stack.Children.Add(hwCard);

            scroll.Content = stack;
            return scroll;
        }

        private void UpdateStatusBadge() {
            if (_hardware.IsConnected) {
                _statusBadge.Background = new SolidColorBrush(Color.FromRgb(20, 60, 35));
                _statusBadgeText.Text = _hardware.IsPureHidMode ? "🛡️ 보안PC 순수 HID 연결됨" : "⚡ 이지블루 연결됨";
                _statusBadgeText.Foreground = new SolidColorBrush(SuccessGreen);
            } else {
                _statusBadge.Background = new SolidColorBrush(Color.FromRgb(50, 35, 20));
                _statusBadgeText.Text = "⚠️ 동글 미연결 (USB 확인)";
                _statusBadgeText.Foreground = new SolidColorBrush(WarningYellow);
            }
        }

        // =========================================================================
        // SYSTEM TRAY & BACKGROUND CONTROLS
        // =========================================================================
        private static System.Drawing.Icon CreateAppIcon() {
            try {
                using (var bmp = new System.Drawing.Bitmap(32, 32)) {
                    using (var g = System.Drawing.Graphics.FromImage(bmp)) {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        // Background circle: deep navy
                        using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(16, 24, 40))) {
                            g.FillEllipse(brush, 1, 1, 30, 30);
                        }
                        // Neon Cyan border
                        using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 220, 255), 2f)) {
                            g.DrawEllipse(pen, 1, 1, 30, 30);
                        }
                        // "EB" Text
                        using (var font = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold))
                        using (var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 220, 255))) {
                            var sf = new System.Drawing.StringFormat {
                                Alignment = System.Drawing.StringAlignment.Center,
                                LineAlignment = System.Drawing.StringAlignment.Center
                            };
                            g.DrawString("EB", font, textBrush, new System.Drawing.RectangleF(0, 0, 32, 32), sf);
                        }
                    }
                    IntPtr hIcon = bmp.GetHicon();
                    return System.Drawing.Icon.FromHandle(hIcon);
                }
            } catch {
                return System.Drawing.SystemIcons.Application;
            }
        }

        private void InitializeTray() {
            var appIcon = CreateAppIcon();
            _trayIcon = new System.Windows.Forms.NotifyIcon {
                Text = "EasyBlue Studio 2026",
                Icon = appIcon,
                Visible = true
            };

            // Left click or double click restores window
            _trayIcon.MouseClick += (s, e) => {
                if (e.Button == System.Windows.Forms.MouseButtons.Left) {
                    RestoreWindow();
                }
            };
            _trayIcon.DoubleClick += (s, e) => RestoreWindow();

            _trayMenu = new System.Windows.Forms.ContextMenuStrip();
            _trayMenu.Opening += (s, e) => RefreshTrayMenuItems();
            _trayIcon.ContextMenuStrip = _trayMenu;

            RefreshTrayMenuItems();
        }

        private void RefreshTrayMenuItems() {
            if (_trayMenu == null) return;
            _trayMenu.Items.Clear();

            // 1. Header (Title & Status)
            var titleItem = new System.Windows.Forms.ToolStripMenuItem("⚡ EasyBlue Studio 2026") {
                Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold),
                Enabled = false
            };
            _trayMenu.Items.Add(titleItem);

            string modeText = _inputHook.IsMobileActive ? "● 📱 현재: 모바일 제어 중" : "● 🖥️ 현재: PC 제어 중";
            var modeItem = new System.Windows.Forms.ToolStripMenuItem(modeText) {
                ForeColor = _inputHook.IsMobileActive ? System.Drawing.Color.FromArgb(0, 180, 255) : System.Drawing.Color.FromArgb(46, 204, 113),
                Enabled = false
            };
            _trayMenu.Items.Add(modeItem);

            _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            // 2. Window Restore
            var openItem = new System.Windows.Forms.ToolStripMenuItem("🖥️ EasyBlue Studio 열기", null, (s, e) => RestoreWindow()) {
                Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold)
            };
            _trayMenu.Items.Add(openItem);

            // 3. Switch & Return Actions
            _trayMenu.Items.Add("🔄 PC ↔ 모바일 전환 (" + _config.HotkeySwitch + ")", null, (s, e) => _inputHook.ToggleMode());
            if (_inputHook.IsMobileActive) {
                _trayMenu.Items.Add("🖥️ PC로 즉시 복귀 (" + _config.HotkeyReturn + " / ESC)", null, (s, e) => _inputHook.SwitchToPC());
            } else {
                _trayMenu.Items.Add("📱 모바일 제어로 전환", null, (s, e) => _inputHook.SwitchToMobile());
            }

            _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            // 4. Bluetooth Pairing & Slots
            _trayMenu.Items.Add("📶 페어링 대기 모드 시작", null, (s, e) => {
                bool ok = _hardware.EnterPairingMode();
                if (ok) {
                    Dispatcher.BeginInvoke(new Action(() => ShowPairingPinDialog()));
                } else {
                    ShowTip("페어링 오류", "동글과 통신할 수 없습니다. USB 연결을 확인하세요.");
                }
            });

            var slotSubmenu = new System.Windows.Forms.ToolStripMenuItem("📱 디바이스 슬롯 선택");
            for (int i = 0; i < 4; i++) {
                int sIdx = i;
                bool isCur = (_config.ActiveSlotIndex == sIdx);
                var slotItem = new System.Windows.Forms.ToolStripMenuItem("슬롯 " + (sIdx + 1) + ": " + _config.DeviceSlots[sIdx]) {
                    Checked = isCur
                };
                slotItem.Click += (s, e) => {
                    _hardware.SelectDeviceSlot(sIdx);
                    _config.ActiveSlotIndex = sIdx;
                    _config.Save();
                    ShowTip("디바이스 전환", "슬롯 " + (sIdx + 1) + "번으로 전환했습니다.");
                };
                slotSubmenu.DropDownItems.Add(slotItem);
            }
            _trayMenu.Items.Add(slotSubmenu);

            // 5. Quick Controls
            _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var ledItem = new System.Windows.Forms.ToolStripMenuItem("💡 동글 LED 켜기") {
                Checked = _config.LedEnabled
            };
            ledItem.Click += (s, e) => {
                _config.LedEnabled = !_config.LedEnabled;
                _hardware.SetLed(_config.LedEnabled);
                _config.Save();
            };
            _trayMenu.Items.Add(ledItem);

            var edgeItem = new System.Windows.Forms.ToolStripMenuItem("🖱️ 마우스 화면 경계 이동 (Flow)") {
                Checked = _config.EdgeTraversalEnabled
            };
            edgeItem.Click += (s, e) => {
                _config.EdgeTraversalEnabled = !_config.EdgeTraversalEnabled;
                _config.Save();
            };
            _trayMenu.Items.Add(edgeItem);

            _trayMenu.Items.Add("🚀 스마트 클립보드 붙여넣기 (" + _config.HotkeyPaste + ")", null, (s, e) => StreamClipboardToMobile());
            _trayMenu.Items.Add("🔌 블루투스 연결 해제 (" + _config.HotkeyDisconnect + ")", null, (s, e) => _hardware.DisconnectDevice());

            // 6. Preferences & Exit
            _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            _trayMenu.Items.Add("⚙️ 환경설정", null, (s, e) => {
                RestoreWindow();
                SwitchTab(3);
            });

            _trayMenu.Items.Add("✕ 완전히 종료", null, (s, e) => {
                _isExplicitExit = true;
                _config.Save();
                _inputHook.Dispose();
                _hardware.Dispose();
                if (_trayIcon != null) {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                }
                Application.Current.Shutdown();
            });
        }

        private void ShowPairingPinDialog() {
            var dlg = new Window {
                Title = "EasyBlue 페어링 PIN 코드 입력",
                Width = 450,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Topmost = true,
                Background = new SolidColorBrush(Color.FromRgb(22, 27, 36)),
                Foreground = Brushes.White
            };

            var root = new StackPanel { Margin = new Thickness(24, 20, 24, 22) };
            root.Children.Add(new TextBlock {
                Text = "📶 블루투스 페어링 대기 중입니다",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248))
            });
            root.Children.Add(new TextBlock {
                Text = "스마트폰에서 'EasyBlue'를 선택하면 화면에 숫자 PIN 코드가 표시됩니다.\n아래 입력창에 해당 PIN 코드를 입력하고 [Enter] 또는 [PIN 전송]을 누르세요.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 192, 210)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 14)
            });

            var pinBox = new TextBox {
                Height = 42,
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(30, 37, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(1.5),
                CaretBrush = Brushes.White,
                MaxLength = 16
            };
            root.Children.Add(pinBox);

            var statusTxt = new TextBlock {
                Text = "대기 상태: 스마트폰에서 'EasyBlue'를 선택하세요.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(0, 10, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            root.Children.Add(statusTxt);

            var btnPanel = new StackPanel {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            var sendBtn = new Button {
                Content = "PIN 전송 (Enter)",
                Width = 140,
                Height = 36,
                Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                IsDefault = true,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var closeBtn = new Button {
                Content = "닫기",
                Width = 84,
                Height = 36,
                Background = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                IsCancel = true,
                Cursor = Cursors.Hand
            };

            bool pinSent = false;
            var pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            pollTimer.Tick += (s, e) => {
                ushort stat = _hardware.GetBcmStatus();
                int connState = stat & 0x03;
                if (pinSent && (connState == 1 || connState == 3)) {
                    pollTimer.Stop();
                    statusTxt.Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128));
                    statusTxt.Text = "✅ 스마트폰과 페어링 및 연결이 완료되었습니다!";
                    ShowTip("페어링 완료", "스마트폰과 연결되었습니다.");
                    var closeDelay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
                    closeDelay.Tick += (s2, e2) => {
                        closeDelay.Stop();
                        dlg.Close();
                    };
                    closeDelay.Start();
                }
            };
            pollTimer.Start();
            dlg.Closed += (s, e) => pollTimer.Stop();

            sendBtn.Click += (s, e) => {
                string pin = pinBox.Text.Trim();
                if (string.IsNullOrEmpty(pin)) {
                    statusTxt.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36));
                    statusTxt.Text = "스마트폰 화면에 표시된 숫자 PIN 코드를 입력하세요.";
                    pinBox.Focus();
                    return;
                }
                sendBtn.IsEnabled = false;
                bool sent = _hardware.SendPinCode(pin);
                if (sent) {
                    pinSent = true;
                    statusTxt.Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128));
                    statusTxt.Text = "PIN 코드(" + pin + ") 전송 완료! 스마트폰의 페어링 완료를 확인 중...";
                    ShowTip("페어링 PIN 전송 완료", "스마트폰의 블루투스 연결 상태를 확인하세요.");
                } else {
                    statusTxt.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
                    statusTxt.Text = "전송 실패: USB 동글 연결 상태를 확인하세요.";
                }
                sendBtn.IsEnabled = true;
            };

            closeBtn.Click += (s, e) => dlg.Close();
            btnPanel.Children.Add(sendBtn);
            btnPanel.Children.Add(closeBtn);
            root.Children.Add(btnPanel);

            dlg.Content = root;
            dlg.Loaded += (s, e) => { pinBox.Focus(); };
            dlg.Show();
            dlg.Activate();
        }

        private void RestoreWindow() {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }

        private void ShowTip(string title, string text) {
            if (_trayIcon != null) {
                _trayIcon.ShowBalloonTip(1500, title, text, System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        protected override void OnClosed(EventArgs e) {
            if (_pollTimer != null) _pollTimer.Stop();
            if (_inputHook != null) _inputHook.Dispose();
            if (_hardware != null) _hardware.Dispose();
            if (_trayIcon != null) {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            if (_mobileIndicatorWindow != null) {
                _mobileIndicatorWindow.Close();
            }
            base.OnClosed(e);
        }
    }
}
