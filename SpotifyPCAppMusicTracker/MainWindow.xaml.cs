using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpotifyPCAppMusicTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Process process;
        private ImageSource off, on;
        private string currentTitle;
        private int currentSessionId;

        private double desiredWidth;
        private double elapsedTime;

        public MainWindow()
        {
            InitializeComponent();
            Timer timer = new Timer(100);
            Timer adjustSize = new Timer(10);

            currentSessionId = Process.GetCurrentProcess().SessionId;
            process = Process.GetProcessesByName("spotify").Concat(Process.GetProcessesByName("Spotify")).FirstOrDefault(p => p.SessionId == currentSessionId);

            if (process == null)
            {
                MessageBox.Show("Spotify Process not found. Exiting...");
                Application.Current.Shutdown();
            }

            off = new BitmapImage(new Uri("pack://application:,,,/img/Sound-off-icon.png"));
            on = new BitmapImage(new Uri("pack://application:,,,/img/Sound-on-icon.png"));

            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            adjustSize.Elapsed += AdjustSize_Elapsed;
            adjustSize.Start();
        }

        private void AdjustSize_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (grid.ActualWidth == this.desiredWidth)
                {
                    elapsedTime = 0;
                    return;
                }
                elapsedTime += 10;

                Console.WriteLine("elapsedTime: " + elapsedTime / 1000d);
                Console.WriteLine("lerp: " + lerp(grid.ActualWidth, desiredWidth, elapsedTime / 1000d));
                grid.Width = Math.Abs(lerp(grid.ActualWidth, desiredWidth, elapsedTime / 1000d));
            });
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            process = Process.GetProcessesByName("spotify").Concat(Process.GetProcessesByName("Spotify")).FirstOrDefault(p => p.SessionId == currentSessionId);
            if (process == null) return;

            string title = process.MainWindowTitle;
            if (currentTitle == title) return;

            currentTitle = title;
            title = title.Replace("Spotify", "").Replace("spotify", "").Trim().TrimStart('-').Trim();
            if (string.IsNullOrEmpty(title))
            {
                lblTitle.Dispatcher.Invoke(() =>
                {
                    lblTitle.Content = "Not Playing";
                    lblAuthor.Content = "Not Playing";
                    img.Source = off;
                    desiredWidth = (MeasureString("Not Playing") + img.ActualWidth);
                    elapsedTime = 0;
                });
            }
            else
            {
                string songName = new string(string.Join(" - ", title.Split(new string[] { " \u2013 ", " - " }, StringSplitOptions.None).Skip(1)).ToArray());
                string authorName = title.Split(new string[] { " \u2013 ", " - " }, StringSplitOptions.None).FirstOrDefault();
                lblTitle.Dispatcher.Invoke(() =>
                {
                    lblTitle.Content = songName;
                    lblAuthor.Content = authorName;
                    img.Source = on;
                    desiredWidth = (MeasureString(songName.Length > authorName.Length ? songName : authorName) + img.ActualWidth);
                    elapsedTime = 0;
                });
            }
        }

        private double MeasureString(string candidate)
        {
            var formattedText = new FormattedText(
                candidate,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(this.lblTitle.FontFamily, this.lblTitle.FontStyle, this.lblTitle.FontWeight, this.lblTitle.FontStretch),
                this.lblTitle.FontSize,
                Brushes.Black);

            return formattedText.Width + 25;
        }

        private double lerp(double source, double dest, double t)
        {
            return (1 - t) * source + t * dest;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.IsHitTestVisible = false;
            grid.IsHitTestVisible = false;
            this.lblAuthor.IsHitTestVisible = false;
            this.lblTitle.IsHitTestVisible = false;
            this.img.IsHitTestVisible = false;
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.SetWindowExTransparent(hwnd);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        public static class WindowsServices
        {
            private const int WS_EX_TRANSPARENT = 0x00000020;
            private const int GWL_EXSTYLE = (-20);

            [DllImport("user32.dll")]
            private static extern int GetWindowLong(IntPtr hwnd, int index);

            [DllImport("user32.dll")]
            private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

            public static void SetWindowExTransparent(IntPtr hwnd)
            {
                var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            }
        }
    }
}