using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace healthkick
{
  /// <summary>
  /// Interaction logic for Notifier.xaml
  /// </summary>
  public partial class Notifier : Window
  {
    public Notifier()
    {
      InitializeComponent();
      
      Box.Width = Message.Width + 30;
      Box.Height = Message.Height + SubMessage.Height;
    }

    public void Hide()
    {
      
    }

  }
}
