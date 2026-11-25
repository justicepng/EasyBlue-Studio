using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
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
                    MessageBox.Show("EasyBlue Studio가 이미 실행 중입니다.", "EasyBlue Studio", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var app = new App();
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                var win = new MainWindow();
                app.Run(win);
            }
        }
    }

    public class MainWindow : Window {
        // Colors & Theme (Windows 11 Dark Mica / Fluent Style)
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

        // Core Engine & Paths
        private string _coreDir;
        private string _ebExePath;
        private Process _ebProcess;
        private DispatcherTimer _statusTimer;

        // UI Controls
        private Border _statusBadge;
        private TextBlock _statusBadgeText;
        private ContentControl _mainContentArea;
        private Button[] _navButtons;
        private int _currentTabIndex = 0;

        // State
        private bool _isDeviceConnected = false;
        private bool _isSecureMode = false;
        private string _phoneEdgePosition = "Right"; // Left, Right, Top, Bottom
        private Canvas _layoutCanvas;
        private Border _phoneVisualCard;
        private TextBlock _phonePosLabel;
        private System.Windows.Forms.NotifyIcon _trayIcon;

        public MainWindow() {
            _coreDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "core");
            _ebExePath = System.IO.Path.Combine(_coreDir, "EBTiny.exe");

            InitializeWindow();
            BuildModernUI();
            InitializeTray();
            ApplyWindows11DpiCompatibility();
            EnsureCoreEngineRunning();

            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(2.5);
            _statusTimer.Tick += (s, e) => {
                CheckHardware();
                EnsureCoreEngineRunning();
            };
            _statusTimer.Start();
            CheckHardware();
        }

        private void InitializeWindow() {
            Title = "EasyBlue Studio 2026";
            Width = 1000;
            Height = 660;
            MinWidth = 880;
            MinHeight = 580;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(BgDark);
            Foreground = new SolidColorBrush(TextPrimary);
            FontFamily = new FontFamily("Malgun Gothic, Segoe UI");

            // Modern borderless window styling with custom title bar
            WindowStyle = WindowStyle.None;
            AllowsTransparency = false;
        }

        private void BuildModernUI() {
            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) }); // Title Bar
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Body

            // 1. Custom Title Bar
            var titleBar = CreateTitleBar();
            Grid.SetRow(titleBar, 0);
            rootGrid.Children.Add(titleBar);

            // 2. Main Body Grid (Sidebar + Content Area)
            var bodyGrid = new Grid();
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) }); // Sidebar
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // View Area

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

            // Load initial page (Device Layout)
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

            // Logo & Title
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

            // Center Status Badge
            _statusBadge = new Border {
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 4, 10, 4),
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 50)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _statusBadgeText = new TextBlock {
                Text = "● 동글 감지 중...",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(TextSecondary)
            };
            _statusBadge.Child = _statusBadgeText;
            Grid.SetColumn(_statusBadge, 1);
            grid.Children.Add(_statusBadge);

            // Window Controls (Minimize, Close)
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var btnMin = CreateIconBtn("─", () => WindowState = WindowState.Minimized);
            var btnClose = CreateIconBtn("✕", () => {
                // Minimize to tray instead of exit
                Hide();
                if (_trayIcon != null) {
                    _trayIcon.ShowBalloonTip(1500, "EasyBlue Studio", "백그라운드 트레이에서 계속 실행 중입니다.", System.Windows.Forms.ToolTipIcon.Info);
                }
            });

            btnPanel.Children.Add(btnMin);
            btnPanel.Children.Add(btnClose);
            Grid.SetColumn(btnPanel, 2);
            grid.Children.Add(btnPanel);

            bar.Child = grid;
            return bar;
        }

        private UIElement CreateSidebar() {
            var panel = new Border {
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 34)),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(12, 16, 12, 16)
            };

            var stack = new StackPanel();

            // Navigation Buttons
            string[] tabs = new string[] {
                "🖥️  화면 및 기기 배치",
                "📋  스마트 클립보드",
                "🎮  모바일 컨트롤 덱",
                "🛡️  보안 PC & 진단",
                "⚙️  설정 및 호환성"
            };

            _navButtons = new Button[tabs.Length];
            for (int i = 0; i < tabs.Length; i++) {
                int index = i;
                var btn = new Button {
                    Content = tabs[i],
                    Height = 44,
                    Margin = new Thickness(0, 0, 0, 6),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(16, 0, 0, 0),
                    Cursor = Cursors.Hand,
                    Background = (i == 0) ? new SolidColorBrush(AccentBlue) : Brushes.Transparent,
                    Foreground = (i == 0) ? Brushes.White : new SolidColorBrush(TextSecondary),
                    BorderThickness = new Thickness(0)
                };

                btn.Click += (s, e) => SwitchTab(index);
                _navButtons[i] = btn;
                stack.Children.Add(btn);
            }

            // Quick Device Switch Cards at bottom of sidebar
            var spacer = new Border { Height = 40 };
            stack.Children.Add(spacer);

            var devTitle = new TextBlock {
                Text = "빠른 단말 전환",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(TextSecondary),
                Margin = new Thickness(8, 0, 0, 8)
            };
            stack.Children.Add(devTitle);

            var devGrid = new Grid();
            devGrid.ColumnDefinitions.Add(new ColumnDefinition());
            devGrid.ColumnDefinitions.Add(new ColumnDefinition());
            devGrid.RowDefinitions.Add(new RowDefinition());
            devGrid.RowDefinitions.Add(new RowDefinition());

            for (int i = 1; i <= 4; i++) {
                int devId = i;
                var dBtn = new Button {
                    Content = "단말 " + i + "\n(Ctrl+F" + i + ")",
                    Height = 42,
                    Margin = new Thickness(2),
                    FontSize = 10.5,
                    Background = new SolidColorBrush(CardBg),
                    Foreground = new SolidColorBrush(TextPrimary),
                    BorderBrush = new SolidColorBrush(BorderColor),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand
                };
                dBtn.Click += (s, e) => SwitchMobileDevice(devId);

                Grid.SetColumn(dBtn, (i - 1) % 2);
                Grid.SetRow(dBtn, (i - 1) / 2);
                devGrid.Children.Add(dBtn);
            }
            stack.Children.Add(devGrid);

            panel.Child = stack;
            return panel;
        }

        private void SwitchTab(int index) {
            _currentTabIndex = index;
            for (int i = 0; i < _navButtons.Length; i++) {
                if (i == index) {
                    _navButtons[i].Background = new SolidColorBrush(AccentBlue);
                    _navButtons[i].Foreground = Brushes.White;
                } else {
                    _navButtons[i].Background = Brushes.Transparent;
                    _navButtons[i].Foreground = new SolidColorBrush(TextSecondary);
                }
            }

            switch (index) {
                case 0: _mainContentArea.Content = CreateLayoutView(); break;
                case 1: _mainContentArea.Content = CreateClipboardView(); break;
                case 2: _mainContentArea.Content = CreateControlDeckView(); break;
                case 3: _mainContentArea.Content = CreateSecurityDiagnosticsView(); break;
                case 4: _mainContentArea.Content = CreateSettingsView(); break;
            }
        }

        // ==========================================
        // TAB 1: INTERACTIVE VIRTUAL LAYOUT CANVAS
        // ==========================================
        private UIElement CreateLayoutView() {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(8, 16, 8, 16) };

            var header = new TextBlock {
                Text = "🖥️ 디바이스 가상 배치 (Logitech Flow / Universal Control 스타일)",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var desc = new TextBlock {
                Text = "PC 모니터 화면 경계 어디로 마우스를 넘겼을 때 스마트폰으로 전환될지 시각적으로 지정합니다.",
                FontSize = 12.5,
                Foreground = new SolidColorBrush(TextSecondary),
                Margin = new Thickness(0, 0, 0, 16)
            };
            stack.Children.Add(header);
            stack.Children.Add(desc);

            // Visual Canvas Container
            var canvasBorder = new Border {
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Height = 270,
                ClipToBounds = true
            };

            _layoutCanvas = new Canvas {
                Width = 680,
                Height = 270,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Main PC Monitor Card
            var pcCard = new Border {
                Width = 240,
                Height = 150,
                Background = new SolidColorBrush(Color.FromRgb(48, 52, 65)),
                BorderBrush = new SolidColorBrush(AccentBlue),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8)
            };
            var pcStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            pcStack.Children.Add(new TextBlock { Text = "💻 메인 PC 화면", FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center });
            pcStack.Children.Add(new TextBlock { Text = string.Format("{0}x{1} (주 모니터)", (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight), FontSize = 11, Foreground = new SolidColorBrush(TextSecondary), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
            pcCard.Child = pcStack;

            Canvas.SetLeft(pcCard, 220);
            Canvas.SetTop(pcCard, 60);
            _layoutCanvas.Children.Add(pcCard);

            // Smartphone Card
            _phoneVisualCard = new Border {
                Width = 110,
                Height = 170,
                Background = new SolidColorBrush(Color.FromRgb(26, 80, 130)),
                BorderBrush = new SolidColorBrush(AccentCyan),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(14),
                Cursor = Cursors.Hand
            };
            var phoneStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            phoneStack.Children.Add(new TextBlock { Text = "📱", FontSize = 28, HorizontalAlignment = HorizontalAlignment.Center });
            phoneStack.Children.Add(new TextBlock { Text = "스마트폰", FontWeight = FontWeights.Bold, FontSize = 12, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center });
            _phonePosLabel = new TextBlock { Text = "(오른쪽)", FontSize = 11, Foreground = new SolidColorBrush(AccentCyan), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };
            phoneStack.Children.Add(_phonePosLabel);
            _phoneVisualCard.Child = phoneStack;

            UpdatePhoneCanvasPosition();
            _layoutCanvas.Children.Add(_phoneVisualCard);
            canvasBorder.Child = _layoutCanvas;
            stack.Children.Add(canvasBorder);

            // Direction Selector Buttons
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };
            btnRow.Children.Add(CreateDirButton("← 왼쪽으로 배치", "Left"));
            btnRow.Children.Add(CreateDirButton("오른쪽으로 배치 →", "Right"));
            btnRow.Children.Add(CreateDirButton("↑ 위쪽으로 배치", "Top"));
            btnRow.Children.Add(CreateDirButton("↓ 아래쪽으로 배치", "Bottom"));
            stack.Children.Add(btnRow);

            // Sensitivity / Transition Control Cards
            var optionCard = new Border {
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 16, 0, 0)
            };
            var optStack = new StackPanel();

            var row1 = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            var lbl1 = new TextBlock { Text = "화면 경계 이동 지연 (Edge Delay - 마우스 튕김 방지):", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
            var slider1 = new Slider { Minimum = 0, Maximum = 500, Value = 100, Width = 180, VerticalAlignment = VerticalAlignment.Center };
            var val1 = new TextBlock { Text = "100 ms", Foreground = new SolidColorBrush(AccentCyan), Width = 60, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            slider1.ValueChanged += (s, e) => val1.Text = (int)slider1.Value + " ms";
            row1.Children.Add(lbl1);
            DockPanel.SetDock(val1, Dock.Right);
            row1.Children.Add(val1);
            DockPanel.SetDock(slider1, Dock.Right);
            row1.Children.Add(slider1);
            optStack.Children.Add(row1);

            var row2 = new DockPanel();
            var lbl2 = new TextBlock { Text = "마우스 커서 가장자리 감도 (Trigger Zone):", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
            var slider2 = new Slider { Minimum = 1, Maximum = 10, Value = 5, Width = 180, VerticalAlignment = VerticalAlignment.Center };
            var val2 = new TextBlock { Text = "5 px", Foreground = new SolidColorBrush(AccentCyan), Width = 60, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            slider2.ValueChanged += (s, e) => val2.Text = (int)slider2.Value + " px";
            row2.Children.Add(lbl2);
            DockPanel.SetDock(val2, Dock.Right);
            row2.Children.Add(val2);
            DockPanel.SetDock(slider2, Dock.Right);
            row2.Children.Add(slider2);
            optStack.Children.Add(row2);

            optionCard.Child = optStack;
            stack.Children.Add(optionCard);

            scroll.Content = stack;
            return scroll;
        }

        private Button CreateDirButton(string text, string dir) {
            var btn = new Button {
                Content = text,
                Height = 36,
                Margin = new Thickness(4, 0, 4, 0),
                Padding = new Thickness(12, 0, 12, 0),
                Background = (_phoneEdgePosition == dir) ? new SolidColorBrush(AccentBlue) : new SolidColorBrush(CardBg),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            btn.Click += (s, e) => {
                _phoneEdgePosition = dir;
                UpdatePhoneCanvasPosition();
                _mainContentArea.Content = CreateLayoutView();
            };
            return btn;
        }

        private void UpdatePhoneCanvasPosition() {
            if (_layoutCanvas == null || _phoneVisualCard == null) return;
            switch (_phoneEdgePosition) {
                case "Left":
                    Canvas.SetLeft(_phoneVisualCard, 60);
                    Canvas.SetTop(_phoneVisualCard, 50);
                    _phonePosLabel.Text = "(왼쪽)";
                    break;
                case "Right":
                    Canvas.SetLeft(_phoneVisualCard, 500);
                    Canvas.SetTop(_phoneVisualCard, 50);
                    _phonePosLabel.Text = "(오른쪽)";
                    break;
                case "Top":
                    Canvas.SetLeft(_phoneVisualCard, 285);
                    Canvas.SetTop(_phoneVisualCard, 0);
                    _phonePosLabel.Text = "(위쪽)";
                    break;
                case "Bottom":
                    Canvas.SetLeft(_phoneVisualCard, 285);
                    Canvas.SetTop(_phoneVisualCard, 190);
                    _phonePosLabel.Text = "(아래쪽)";
                    break;
            }
        }

        // ==========================================
        // TAB 2: SMART CLIPBOARD & TEXT STREAMER
        // ==========================================
        private UIElement CreateClipboardView() {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(8, 16, 8, 16) };

            var header = new TextBlock {
                Text = "📋 스마트 클립보드 & 텍스트 스트리머",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var desc = new TextBlock {
                Text = "한글 자모 분리('ㅎㅏㄴㄱㅡㄹ') 및 텍스트 누락 없는 스마트 모바일 타이핑 주입 허브입니다.",
                FontSize = 12.5,
                Foreground = new SolidColorBrush(TextSecondary),
                Margin = new Thickness(0, 0, 0, 16)
            };
            stack.Children.Add(header);
            stack.Children.Add(desc);

            // Real-time Clipboard Preview Card
            var clipPreviewCard = new Border {
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 16)
            };
            var cpStack = new StackPanel();
            cpStack.Children.Add(new TextBlock { Text = "현재 PC 클립보드 복사 내용 미리보기", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(AccentCyan), FontSize = 12, Margin = new Thickness(0, 0, 0, 6) });
            string clipText = "(클립보드가 비어 있습니다)";
            try {
                if (Clipboard.ContainsText()) clipText = Clipboard.GetText();
            } catch { }
            var clipContent = new TextBlock {
                Text = clipText,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxHeight = 60,
                Foreground = new SolidColorBrush(TextPrimary),
                FontSize = 12
            };
            cpStack.Children.Add(clipContent);
            clipPreviewCard.Child = cpStack;
            stack.Children.Add(clipPreviewCard);

            // Direct Mobile Text Injection Box
            var inputTitle = new TextBlock { Text = "모바일로 바로 전송할 텍스트 (URL, 계좌번호, 장문, 코드 등):", FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 6) };
            stack.Children.Add(inputTitle);

            var txtInput = new TextBox {
                Height = 110,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Background = new SolidColorBrush(Color.FromRgb(28, 28, 35)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 12)
            };
            stack.Children.Add(txtInput);

            // Action Buttons
            var btnBar = new StackPanel { Orientation = Orientation.Horizontal };
            var btnSend = new Button {
                Content = "🚀 스마트폰으로 텍스트 주입 전송 (Ctrl + Enter)",
                Height = 40,
                Padding = new Thickness(20, 0, 20, 0),
                Background = new SolidColorBrush(AccentBlue),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnSend.Click += (s, e) => {
                if (!string.IsNullOrEmpty(txtInput.Text)) {
                    StreamTextToPhone(txtInput.Text);
                    txtInput.Clear();
                } else {
                    MessageBox.Show("전송할 텍스트를 입력해 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };

            var btnPasteWin = new Button {
                Content = "📋 이지블루 순정 붙여넣기 창 열기 (Ctrl+Alt+P)",
                Height = 40,
                Margin = new Thickness(12, 0, 0, 0),
                Padding = new Thickness(16, 0, 16, 0),
                Background = new SolidColorBrush(CardBgHover),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            btnPasteWin.Click += (s, e) => {
                SendCtrlAltP();
            };

            btnBar.Children.Add(btnSend);
            btnBar.Children.Add(btnPasteWin);
            stack.Children.Add(btnBar);

            // IME Preset Info Card
            var imeCard = new Border {
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 20, 0, 0)
            };
            var imeStack = new StackPanel();
            imeStack.Children.Add(new TextBlock { Text = "💡 모바일 한/영 전환 팁", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(AccentCyan), Margin = new Thickness(0, 0, 0, 6) });
            imeStack.Children.Add(new TextBlock { Text = "• 삼성 갤럭시: Shift + Space 또는 기본 한/영 키로 전환", FontSize = 12, Foreground = new SolidColorBrush(TextSecondary) });
            imeStack.Children.Add(new TextBlock { Text = "• 신형 안드로이드 / iOS (아이폰/아이패드): Win + Space 또는 CapsLock으로 전환", FontSize = 12, Foreground = new SolidColorBrush(TextSecondary), Margin = new Thickness(0, 2, 0, 0) });
            imeCard.Child = imeStack;
            stack.Children.Add(imeCard);

            scroll.Content = stack;
            return scroll;
        }

        // ==========================================
        // TAB 3: MOBILE REMOTE CONTROL DECK
        // ==========================================
        private UIElement CreateControlDeckView() {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(8, 16, 8, 16) };

            var header = new TextBlock {
                Text = "🎮 모바일 원격 컨트롤 덱 (Stream Deck 스타일)",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var desc = new TextBlock {
                Text = "PC에서 원클릭으로 스마트폰 내비게이션, 홈 화면, 볼륨 및 미디어를 즉시 제어합니다.",
                FontSize = 12.5,
                Foreground = new SolidColorBrush(TextSecondary),
                Margin = new Thickness(0, 0, 0, 16)
            };
            stack.Children.Add(header);
            stack.Children.Add(desc);

            // Deck Grid
            var grid = new Grid();
            for (int r = 0; r < 3; r++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(85) });
            for (int c = 0; c < 4; c++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Buttons matrix
            var deckItems = new[] {
                new { Icon = "🏠", Label = "홈 (Home)", Cmd = "HOME", Row = 0, Col = 0 },
                new { Icon = "◀️", Label = "뒤로가기 (Back)", Cmd = "BACK", Row = 0, Col = 1 },
                new { Icon = "🗂️", Label = "최근 앱 (Recent)", Cmd = "RECENTS", Row = 0, Col = 2 },
                new { Icon = "🔒", Label = "화면 끄기 / 켜기", Cmd = "POWER", Row = 0, Col = 3 },

                new { Icon = "🔊", Label = "볼륨 증가 (+)", Cmd = "VOL_UP", Row = 1, Col = 0 },
                new { Icon = "🔉", Label = "볼륨 감소 (-)", Cmd = "VOL_DOWN", Row = 1, Col = 1 },
                new { Icon = "🔇", Label = "음소거 (Mute)", Cmd = "MUTE", Row = 1, Col = 2 },
                new { Icon = "📸", Label = "화면 캡처", Cmd = "SCREENSHOT", Row = 1, Col = 3 },

                new { Icon = "⏮️", Label = "이전 곡", Cmd = "MEDIA_PREV", Row = 2, Col = 0 },
                new { Icon = "⏯️", Label = "재생 / 일시정지", Cmd = "MEDIA_PLAY", Row = 2, Col = 1 },
                new { Icon = "⏭️", Label = "다음 곡", Cmd = "MEDIA_NEXT", Row = 2, Col = 2 },
                new { Icon = "🌐", Label = "한/영 토글", Cmd = "LANG_TOGGLE", Row = 2, Col = 3 }
            };

            foreach (var item in deckItems) {
                var btn = new Button {
                    Margin = new Thickness(4),
                    Background = new SolidColorBrush(CardBg),
                    BorderBrush = new SolidColorBrush(BorderColor),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand
                };
                var p = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                p.Children.Add(new TextBlock { Text = item.Icon, FontSize = 22, HorizontalAlignment = HorizontalAlignment.Center });
                p.Children.Add(new TextBlock { Text = item.Label, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(TextPrimary), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
                btn.Content = p;

                string cmd = item.Cmd;
                btn.Click += (s, e) => ExecuteMobileDeckCommand(cmd);

                Grid.SetRow(btn, item.Row);
                Grid.SetColumn(btn, item.Col);
                grid.Children.Add(btn);
            }

            stack.Children.Add(grid);
            scroll.Content = stack;
            return scroll;
        }

        // ==========================================
        // TAB 4: SECURITY PC & DIAGNOSTICS HUB
        // ==========================================
        private UIElement CreateSecurityDiagnosticsView() {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(8, 16, 8, 16) };

            var header = new TextBlock {
                Text = "🛡️ 사내 보안 PC & 하드웨어 진단 허브",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var desc = new TextBlock {
                Text = "사내 보안 프로그램(DLP/매체제어) 차단 여부를 검사하고 펌웨어를 안전하게 관리합니다.",
                FontSize = 12.5,
                Foreground = new SolidColorBrush(TextSecondary),
                Margin = new Thickness(0, 0, 0, 16)
            };
            stack.Children.Add(header);
            stack.Children.Add(desc);

            // Hardware Telemetry Card
            var hwCard = new Border {
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18),
                Margin = new Thickness(0, 0, 0, 16)
            };
            var hwStack = new StackPanel();
            hwStack.Children.Add(new TextBlock { Text = "USB 동글 하드웨어 감지 상태", FontWeight = FontWeights.Bold, FontSize = 14, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 10) });

            string hwStatus = _isDeviceConnected ? (_isSecureMode ? "🟢 [보안PC 모드] 연결됨 - 가상 드라이브 완전 차단됨 (사내 DLP 안전)" : "🟡 [일반 모드] 연결됨 - 가상 드라이브(D:) 마운트됨") : "🔴 [미연결] USB 동글을 찾을 수 없습니다.";
            var statusBox = new Border {
                Background = new SolidColorBrush(_isSecureMode ? Color.FromRgb(15, 60, 40) : Color.FromRgb(40, 40, 50)),
                BorderBrush = new SolidColorBrush(_isSecureMode ? SuccessGreen : BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10)
            };
            statusBox.Child = new TextBlock { Text = hwStatus, FontWeight = FontWeights.Bold, Foreground = _isSecureMode ? new SolidColorBrush(SuccessGreen) : Brushes.White, FontSize = 12.5 };
            hwStack.Children.Add(statusBox);

            hwStack.Children.Add(new TextBlock { Text = "• 장치 식별자: USB\\VID_1D54&PID_B002 (Zaram Smart BKM Switch)", FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary) });
            hwStack.Children.Add(new TextBlock { Text = "• 통신 프로토콜: USB HID (UsagePage: 0xFFA0, Usage: 0x0001 - Driverless)", FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary), Margin = new Thickness(0, 2, 0, 0) });
            hwStack.Children.Add(new TextBlock { Text = "• 블루투스 칩셋: Broadcom BCM Series (안드로이드/iOS 무선 페어링)", FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary), Margin = new Thickness(0, 2, 0, 0) });

            hwCard.Child = hwStack;
            stack.Children.Add(hwCard);

            // Firmware Tools Button Group
            var fwCard = new Border {
                Background = new SolidColorBrush(CardBg),
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18)
            };
            var fwStack = new StackPanel();
            fwStack.Children.Add(new TextBlock { Text = "펌웨어 전환 도구 (개인 PC에서 수행 권장)", FontWeight = FontWeights.Bold, FontSize = 14, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 8) });
            fwStack.Children.Add(new TextBlock { Text = "사내 PC에서 쓸 수 있도록 동글의 외장 드라이브를 끄거나(보안PC용), 다시 원래대로 복구합니다.", FontSize = 12, Foreground = new SolidColorBrush(TextSecondary), Margin = new Thickness(0, 0, 0, 12) });

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal };
            var btnSec = new Button {
                Content = "🛡️ [보안PC용 DSP-232 전환] 실행",
                Height = 38,
                Padding = new Thickness(16, 0, 16, 0),
                Background = new SolidColorBrush(Color.FromRgb(20, 110, 180)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnSec.Click += (s, e) => LaunchFirmwareScript("[보안PC용_펌웨어_전환].bat");

            var btnNorm = new Button {
                Content = "🔄 [일반용 DSP-220 복구] 실행",
                Height = 38,
                Margin = new Thickness(12, 0, 0, 0),
                Padding = new Thickness(16, 0, 16, 0),
                Background = new SolidColorBrush(CardBgHover),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            btnNorm.Click += (s, e) => LaunchFirmwareScript("[일반용_펌웨어_복구].bat");

            btnRow.Children.Add(btnSec);
            btnRow.Children.Add(btnNorm);
            fwStack.Children.Add(btnRow);
            fwCard.Child = fwStack;
            stack.Children.Add(fwCard);

            scroll.Content = stack;
            return scroll;
        }

        // ==========================================
        // TAB 5: SETTINGS & COMPATIBILITY
        // ==========================================
        private UIElement CreateSettingsView() {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(8, 16, 8, 16) };

            var header = new TextBlock {
                Text = "⚙️ 환경 설정 및 Windows 11 호환성",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var desc = new TextBlock {
                Text = "관리자 권한 자동 실행, DPI 보정 및 동작 옵션을 구성합니다.",
                FontSize = 12.5,
                Foreground = new SolidColorBrush(TextSecondary),
                Margin = new Thickness(0, 0, 0, 16)
            };
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

            // Setting 1: Auto Start via Task Scheduler
            var s1 = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
            var s1Text = new StackPanel();
            s1Text.Children.Add(new TextBlock { Text = "Windows 시작 시 관리자 권한으로 자동 실행", FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            s1Text.Children.Add(new TextBlock { Text = "작업 스케줄러(schtasks)의 최고 권한으로 등록하여 부팅 시 UAC 팝업 없이 백그라운드 자동 실행", FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary) });
            s1.Children.Add(s1Text);

            var s1Btn = new Button {
                Content = "자동 시작 등록 / 토글",
                Height = 32,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush(AccentBlue),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            s1Btn.Click += (s, e) => ToggleAutoStart();
            DockPanel.SetDock(s1Btn, Dock.Right);
            s1.Children.Add(s1Btn);
            cardStack.Children.Add(s1);

            // Setting 2: DPI compatibility
            var s2 = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
            var s2Text = new StackPanel();
            s2Text.Children.Add(new TextBlock { Text = "Windows 11 Per-Monitor DPI Aware v2 호환성 강제 적용", FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            s2Text.Children.Add(new TextBlock { Text = "레지스트리에 HIGHDPIAWARE 및 RUNASADMIN 플래그를 영구 주입하여 마우스 걸림 방지", FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary) });
            s2.Children.Add(s2Text);

            var s2Btn = new Button {
                Content = "호환성 플래그 재적용",
                Height = 32,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush(CardBgHover),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(BorderColor),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            s2Btn.Click += (s, e) => {
                ApplyWindows11DpiCompatibility();
                MessageBox.Show("DPI 호환성 플래그가 재적용되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            DockPanel.SetDock(s2Btn, Dock.Right);
            s2.Children.Add(s2Btn);
            cardStack.Children.Add(s2);

            // Setting 3: Restart Engine
            var s3 = new DockPanel();
            var s3Text = new StackPanel();
            s3Text.Children.Add(new TextBlock { Text = "이지블루 백그라운드 코어 엔진 재시작", FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            s3Text.Children.Add(new TextBlock { Text = "연결이 끊기거나 마우스가 반응하지 않을 때 엔진을 강제 클린 재시작", FontSize = 11.5, Foreground = new SolidColorBrush(TextSecondary) });
            s3.Children.Add(s3Text);

            var s3Btn = new Button {
                Content = "🔄 코어 재시작",
                Height = 32,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush(Color.FromRgb(180, 50, 50)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            s3Btn.Click += (s, e) => RestartCore();
            DockPanel.SetDock(s3Btn, Dock.Right);
            s3.Children.Add(s3Btn);
            cardStack.Children.Add(s3);

            card.Child = cardStack;
            stack.Children.Add(card);

            scroll.Content = stack;
            return scroll;
        }

        // ==========================================
        // ACTION HELPERS & LOGIC
        // ==========================================
        private void StreamTextToPhone(string text) {
            try {
                // Focus phone or inject via clipboard
                Clipboard.SetText(text);
                SendCtrlAltP();
                if (_trayIcon != null) {
                    _trayIcon.ShowBalloonTip(1500, "텍스트 주입", "스마트폰으로 텍스트 전송 명령을 보냈습니다.", System.Windows.Forms.ToolTipIcon.Info);
                }
            } catch (Exception ex) {
                MessageBox.Show("텍스트 주입 실패: " + ex.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SendCtrlAltP() {
            // Virtual keystroke injection or hotkey
            try {
                keybd_event(0x11, 0, 0, 0); // Ctrl down
                keybd_event(0x12, 0, 0, 0); // Alt down
                keybd_event(0x50, 0, 0, 0); // P down
                keybd_event(0x50, 0, 2, 0); // P up
                keybd_event(0x12, 0, 2, 0); // Alt up
                keybd_event(0x11, 0, 2, 0); // Ctrl up
            } catch { }
        }

        private void SwitchMobileDevice(int devId) {
            byte vk = (byte)(0x6F + devId); // F1 = 0x70
            try {
                keybd_event(0x11, 0, 0, 0); // Ctrl down
                keybd_event(vk, 0, 0, 0);   // F1~F4 down
                keybd_event(vk, 0, 2, 0);   // F1~F4 up
                keybd_event(0x11, 0, 2, 0); // Ctrl up

                if (_trayIcon != null) {
                    _trayIcon.ShowBalloonTip(1000, "단말 전환", "단말 " + devId + "번 전환 신호를 보냈습니다.", System.Windows.Forms.ToolTipIcon.Info);
                }
            } catch { }
        }

        private void ExecuteMobileDeckCommand(string cmd) {
            // Android / Mobile remote shortcuts
            switch (cmd) {
                case "HOME":
                    // Win key / Alt+Home
                    keybd_event(0x5B, 0, 0, 0); keybd_event(0x5B, 0, 2, 0);
                    break;
                case "BACK":
                    // ESC
                    keybd_event(0x1B, 0, 0, 0); keybd_event(0x1B, 0, 2, 0);
                    break;
                case "RECENTS":
                    // Alt+Tab
                    keybd_event(0x12, 0, 0, 0); keybd_event(0x09, 0, 0, 0);
                    keybd_event(0x09, 0, 2, 0); keybd_event(0x12, 0, 2, 0);
                    break;
                case "VOL_UP":
                    keybd_event(0xAF, 0, 0, 0); keybd_event(0xAF, 0, 2, 0);
                    break;
                case "VOL_DOWN":
                    keybd_event(0xAE, 0, 0, 0); keybd_event(0xAE, 0, 2, 0);
                    break;
                case "MUTE":
                    keybd_event(0xAD, 0, 0, 0); keybd_event(0xAD, 0, 2, 0);
                    break;
                case "MEDIA_PLAY":
                    keybd_event(0xB3, 0, 0, 0); keybd_event(0xB3, 0, 2, 0);
                    break;
                case "MEDIA_PREV":
                    keybd_event(0xB1, 0, 0, 0); keybd_event(0xB1, 0, 2, 0);
                    break;
                case "MEDIA_NEXT":
                    keybd_event(0xB0, 0, 0, 0); keybd_event(0xB0, 0, 2, 0);
                    break;
                case "LANG_TOGGLE":
                    // Shift+Space
                    keybd_event(0x10, 0, 0, 0); keybd_event(0x20, 0, 0, 0);
                    keybd_event(0x20, 0, 2, 0); keybd_event(0x10, 0, 2, 0);
                    break;
            }
        }

        private void LaunchFirmwareScript(string scriptName) {
            string baseDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\02_보안PC_펌웨어_전환도구");
            string scriptPath = System.IO.Path.Combine(baseDir, scriptName);
            if (File.Exists(scriptPath)) {
                Process.Start(new ProcessStartInfo {
                    FileName = scriptPath,
                    WorkingDirectory = baseDir,
                    UseShellExecute = true
                });
            } else {
                MessageBox.Show("스크립트를 찾을 수 없습니다: " + scriptPath, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckHardware() {
            try {
                bool foundUsb = false;
                bool hasDrive = false;

                var drives = DriveInfo.GetDrives();
                foreach (var d in drives) {
                    if (d.IsReady) {
                        try {
                            if (File.Exists(System.IO.Path.Combine(d.RootDirectory.FullName, @"Win\EBTiny.exe"))) {
                                hasDrive = true;
                                break;
                            }
                        } catch { }
                    }
                }

                // Check WMI
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT DeviceID FROM Win32_PnPEntity WHERE DeviceID LIKE '%VID_1D54&PID_B002%'")) {
                    foreach (var dev in searcher.Get()) {
                        foundUsb = true;
                        break;
                    }
                }

                _isDeviceConnected = foundUsb;
                _isSecureMode = foundUsb && !hasDrive;

                if (!_isDeviceConnected) {
                    _statusBadgeText.Text = "● 동글 미연결";
                    _statusBadgeText.Foreground = new SolidColorBrush(Color.FromRgb(240, 80, 80));
                    _statusBadge.Background = new SolidColorBrush(Color.FromRgb(50, 25, 25));
                } else if (_isSecureMode) {
                    _statusBadgeText.Text = "● 보안PC 모드 (순수 HID / DLP 안전)";
                    _statusBadgeText.Foreground = new SolidColorBrush(AccentCyan);
                    _statusBadge.Background = new SolidColorBrush(Color.FromRgb(15, 50, 70));
                } else {
                    _statusBadgeText.Text = "● 일반 모드 (가상 드라이브)";
                    _statusBadgeText.Foreground = new SolidColorBrush(SuccessGreen);
                    _statusBadge.Background = new SolidColorBrush(Color.FromRgb(15, 50, 25));
                }
            } catch { }
        }

        private void EnsureCoreEngineRunning() {
            try {
                if (_ebProcess == null || _ebProcess.HasExited) {
                    var procs = Process.GetProcessesByName("EBTiny");
                    if (procs.Length > 0) {
                        _ebProcess = procs[0];
                        return;
                    }

                    if (File.Exists(_ebExePath)) {
                        var psi = new ProcessStartInfo {
                            FileName = _ebExePath,
                            WorkingDirectory = _coreDir,
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Minimized
                        };
                        _ebProcess = Process.Start(psi);
                    }
                }
            } catch { }
        }

        private void RestartCore() {
            try {
                var procs = Process.GetProcessesByName("EBTiny");
                foreach (var p in procs) {
                    try { p.Kill(); p.WaitForExit(1500); } catch { }
                }
                EnsureCoreEngineRunning();
                MessageBox.Show("코어 엔진이 재시작되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            } catch (Exception ex) {
                MessageBox.Show("재시작 실패: " + ex.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyWindows11DpiCompatibility() {
            try {
                string appCompatKey = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";
                using (var key = Registry.CurrentUser.CreateSubKey(appCompatKey)) {
                    if (key != null && File.Exists(_ebExePath)) {
                        key.SetValue(_ebExePath, "~ HIGHDPIAWARE RUNASADMIN");
                    }
                }
            } catch { }
        }

        private void ToggleAutoStart() {
            try {
                string taskName = "EasyBlueStudio_AutoStart";
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                var psiCheck = new ProcessStartInfo("schtasks.exe", "/query /tn \"" + taskName + "\"") {
                    CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true
                };
                var proc = Process.Start(psiCheck);
                proc.WaitForExit();

                if (proc.ExitCode == 0) {
                    var pDel = Process.Start(new ProcessStartInfo("schtasks.exe", "/delete /tn \"" + taskName + "\" /f") {
                        CreateNoWindow = true, UseShellExecute = true, Verb = "runas"
                    });
                    if (pDel != null) pDel.WaitForExit();
                    MessageBox.Show("자동 시작 등록이 해제되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                } else {
                    string cmd = string.Format("/create /tn \"{0}\" /tr \"\\\"{1}\\\"\" /sc onlogon /rl highest /f", taskName, exePath);
                    var pAdd = Process.Start(new ProcessStartInfo("schtasks.exe", cmd) {
                        CreateNoWindow = true, UseShellExecute = true, Verb = "runas"
                    });
                    if (pAdd != null) pAdd.WaitForExit();
                    MessageBox.Show("Windows 시작 시 관리자 권한 자동 실행으로 등록되었습니다!\n(UAC 팝업 없이 백그라운드 자동 실행)", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            } catch (Exception ex) {
                MessageBox.Show("설정 실패: " + ex.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeTray() {
            _trayIcon = new System.Windows.Forms.NotifyIcon();
            _trayIcon.Text = "EasyBlue Studio 2026";
            _trayIcon.Visible = true;

            // Generate modern tray icon
            using (var bmp = new System.Drawing.Bitmap(32, 32))
            using (var g = System.Drawing.Graphics.FromImage(bmp)) {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 140, 255))) {
                    g.FillEllipse(brush, 2, 2, 28, 28);
                }
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.White, 3)) {
                    g.DrawLine(pen, 16, 6, 16, 26);
                    g.DrawLine(pen, 16, 6, 22, 12);
                    g.DrawLine(pen, 22, 12, 10, 20);
                    g.DrawLine(pen, 10, 12, 22, 20);
                    g.DrawLine(pen, 22, 20, 16, 26);
                }
                _trayIcon.Icon = System.Drawing.Icon.FromHandle(bmp.GetHicon());
            }

            var cms = new System.Windows.Forms.ContextMenuStrip();
            cms.Items.Add("대시보드 열기", null, (s, e) => {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            });
            cms.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            cms.Items.Add("완전 종료", null, (s, e) => {
                _statusTimer.Stop();
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                try { if (_ebProcess != null && !_ebProcess.HasExited) _ebProcess.Kill(); } catch { }
                Application.Current.Shutdown();
            });

            _trayIcon.ContextMenuStrip = cms;
            _trayIcon.DoubleClick += (s, e) => {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            };
        }

        private Button CreateIconBtn(string icon, Action clickAction) {
            var btn = new Button {
                Content = icon,
                Width = 36,
                Height = 32,
                Margin = new Thickness(2, 0, 0, 0),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(TextSecondary),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 13,
                FontWeight = FontWeights.Bold
            };
            btn.Click += (s, e) => clickAction();
            return btn;
        }

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
    }
}
