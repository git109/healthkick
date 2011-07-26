using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HealthKick
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    bool closing = false;
    private Storyboard sb;
    public void CloseSticker(int ticks)
    {
      Storyboard sb = (Storyboard)Resources["Peel"];
      sb.BeginTime = TimeSpan.FromMilliseconds(ticks);
      sb.Begin();
      closing = true;
      sb.Completed += new EventHandler(sb_Completed);
    }

    public void ShowSticker()
    {

    }

    private ViewModel.StickerViewModel _vm;

    public MainWindow()
    {
      InitializeComponent();
      sb = (Storyboard)Resources["PeakBack"];
      sb.Begin();
      sb.Pause();
      sb.Seek(TimeSpan.FromSeconds(0));
      this.MouseEnter += new MouseEventHandler(MainWindow_MouseEnter);
      this.MouseLeave += new MouseEventHandler(MainWindow_MouseLeave);
    }

    void MainWindow_MouseLeave(object sender, MouseEventArgs e)
    {
      if (!closing)
      {
        Storyboard sb = (Storyboard)Resources["PeakBack"];
        sb.Begin();
      }
    }

    void MainWindow_MouseEnter(object sender, MouseEventArgs e)
    {
      if (!closing)
      {
        Storyboard sb = (Storyboard)Resources["Peak"];
        //Storyboard.SetTarget(sb.Children.ElementAt(0) as DoubleAnimation, path1);
        sb.Begin();
      }
    }

    public void PlayStoryboard()
    {
      Storyboard sb = (Storyboard)FindResource("animate");
      sb.Begin(stickerPanel);
    }


    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
      base.OnMouseLeftButtonDown(e);
      //DragMove();
      _vm.OnStickerClick(e);
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
      base.OnMouseRightButtonDown(e);
      //CloseSticker(0);
      _vm.HideAllStickers();
      //_vm.RunPythonCode();
    }

    void sb_Completed(object sender, EventArgs e)
    {
      _vm.Destroy(this);
      _vm.Closed(this);
      this.Close();
    }

    protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
    {
      base.OnMouseDoubleClick(e);
      _vm.NewSticker(); // TODO remove
      //PlayStoryboard();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      myGrid.DataContext = _vm;
      // TODO MakeClickThrough();
      sb.Resume();
    }

    private void Button1Click(object sender, RoutedEventArgs e)
    {
      //_vm.State.Color = "Blue";
      _vm.HideAllStickers();
      //_vm.State.LastReading = DateTime.Now;
      //_vm.RunPythonCode();
    }

    public void SetViewModel(ViewModel.StickerViewModel stickerViewModel)
    {
      _vm = stickerViewModel;
    }

    public void MakeClickThrough()
    {
      // Get this window's handle         
      IntPtr hwnd = new WindowInteropHelper(this).Handle;
      // Change the extended window style to include WS_EX_TRANSPARENT         
      int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
      SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
    }

    public void MakeClickable()
    {
      IntPtr hwnd = new WindowInteropHelper(this).Handle;
      int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
      SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
    }

    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int GWL_EXSTYLE = (-20);

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

  }

}


