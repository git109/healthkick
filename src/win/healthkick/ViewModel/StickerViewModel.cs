using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace HealthKick.ViewModel
{

  #region StickerStatus enum

  public enum StickerStatus
  {
    Idle,
    WaitingForReading,
    DownloadingReading,
    DisplayingReading,
    NoNewReading,
    Error
  }

  #endregion

  public class OKCommand : ICommand
  {
    private readonly StickerViewModel vm;

    public OKCommand(StickerViewModel vm)
    {
      this.vm = vm;
    }

    #region ICommand Members

    public void Execute(object parameter)
    {
      vm.HideAllStickers();
    }

    public bool CanExecute(object parameter)
    {
      return true;
    }

    public event EventHandler CanExecuteChanged;

    #endregion
  }

  public class StickerViewModel : INotifyPropertyChanged
  {
    public DAO Dao;
    public Notifier Notifier;
    public ICommand OkCommand;
    public Timer Timer;
    public Timer Timer1;
    public User User;
    public Stack<MainWindow> Windows = new Stack<MainWindow>();
    private string _color;
    public StickerStatus _currentStatus;
    private DateTime _lastReading;
    private double _lastReadingValue;
    private DateTime _lastUpdate;
    private string _msg;
    private bool _showButton;

    public StickerViewModel(DAO dao)
    {
      Dao = dao;
      User = dao.GetUser();
      //State = new StickerState();
      //LastReadingValue = 24.6;
      SetStatus(StickerStatus.WaitingForReading);
      OkCommand = new OKCommand(this);

      // start checking for status updates
      Timer = new Timer(Tick, null, 0, 1000);

      Notifier = new Notifier();
      Notifier.DataContext = this;

      UpdateSticker();
    }

    public ICommand OnButtonClick
    {
      get { return OkCommand; }
    }

    public DateTime LastReading
    {
      get { return _lastReading; }
      set
      {
        _lastReading = value;
        OnPropertyChanged("LastReading");
      }
    }

    public string Msg
    {
      get { return _msg; }
      set
      {
        _msg = value;
        OnPropertyChanged("Msg");
      }
    }

    public String Color
    {
      get { return _color; }
      set
      {
        _color = value;
        OnPropertyChanged("Color");
      }
    }

    public StickerStatus CurrentStatus
    {
      get { return _currentStatus; }
      set
      {
        _currentStatus = value;
        OnPropertyChanged("CurrentStatus");
      }
    }

    public double LastReadingValue
    {
      get { return _lastReadingValue; }
      set
      {
        _lastReadingValue = value;
        OnPropertyChanged("LastReadingValue");
      }
    }

    public bool ShowButton
    {
      get { return _showButton; }
      set
      {
        _showButton = value;
        OnPropertyChanged("ShowButton");
      }
    }

    #region INotifyPropertyChanged Members

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion

    public void SetStatus(StickerStatus state)
    {
      CurrentStatus = state;
      switch (state)
      {
        case StickerStatus.Idle:
          Color = "#E88801";
          HideAllStickers();
          break;
        case StickerStatus.WaitingForReading:
          Color = "#E88801";
          Msg = "waiting for glucose reading";
          break;
        case StickerStatus.DownloadingReading:
          Color = "#FFC200";
          Msg = "downloading reading";
          break;
        case StickerStatus.DisplayingReading:
          if (LastReadingValue < 10)
          {
            Color = "#80BB11";
          }
          else
          {
            Color = "#C93C00";
          }
          ShowButton = true;
          break;
        case StickerStatus.NoNewReading:
          // Fade to WaitingForReading if min time since last reading
          Timer1 = new Timer(UpdateState, null, 5000, 0);
          break;
      }
    }

    public void UpdateState(object o)
    {
      Timer1.Dispose();
      App.GetLatestReadingTime();
      SetStatus(StickerStatus.WaitingForReading);
    }

    public void UpdateSticker()
    {
      LastReading = App.GetLatestReadingTime();
    }

    public void HideAllStickers()
    {
      int ticks = 0;
      while (Windows.Count > 0)
      {
        Windows.Pop().CloseSticker(ticks);
        ticks += 500;
      }
    }

    public void StartTimer()
    {
    }

    public void Tick(object e)
    {
      //Console.WriteLine("Tick!");
      TimeSpan ts = DateTime.Now.Subtract(LastReading);
      TimeSpan timeSinceLastUpdate = DateTime.Now.Subtract(_lastUpdate);

      // Do we want to update the 'time since last reading' caption
      if (timeSinceLastUpdate > TimeSpan.FromSeconds(5))
      {
        UpdateSticker();
        _lastUpdate = DateTime.Now;

        // Add a sticker if more than 6 hours since last reading
        if (ts.Hours > 8)
        {
          if (Windows.Count < 3)
            NewStickerThreadSafeDelegate();
        } else if (ts.Hours > 7)
        {
          if (Windows.Count < 2)
            NewStickerThreadSafeDelegate();
        }
        else if (ts.Hours > 6)
        {
          if (Windows.Count < 1)
            NewStickerThreadSafeDelegate();
        }
        else
        {
          if (Windows.Count > 0)
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
              new Action(() => HideAllStickers()));
        }
      }
    }


    public void Closed(MainWindow mw)
    {
    }

    public void NewSticker()
    {
      var mw = new MainWindow();
      mw.SetViewModel(this);
      mw.Show();
      var rand = new Random();
      if (Windows.Count < SystemParameters.PrimaryScreenHeight/250)
      {
        if (Windows.Count%2 == 0)
        {
          mw.Left = SystemParameters.PrimaryScreenWidth - rand.Next(220, 300);
        }
        else
        {
          mw.Left = SystemParameters.PrimaryScreenWidth - rand.Next(300, 350);
        }
        mw.Top = SystemParameters.PrimaryScreenHeight - 300 - (Windows.Count*150);
      }
      else
      {
        mw.Left = SystemParameters.PrimaryScreenWidth/2 - rand.Next(200, 400);
        mw.Top = SystemParameters.PrimaryScreenHeight/2 - rand.Next(200, 400);
      }
      Windows.Push(mw);
    }

    public void Destroy(MainWindow window)
    {
      Console.WriteLine("Removed");
    }

    public void RunPythonCode()
    {
      var setup = new ScriptRuntimeSetup();
      setup.LanguageSetups.Add(Python.CreateLanguageSetup(null));
      var runtime = new ScriptRuntime(setup);
      runtime.IO.RedirectToConsole();
      ScriptEngine engine = runtime.GetEngine("IronPython");
      ScriptScope scope = engine.CreateScope();
      //ScriptSource source = engine.CreateScriptSourceFromString("print 'Hello World'\r\na=1", SourceCodeKind.Statements);
      ScriptSource source = engine.CreateScriptSourceFromFile("glucodump/main.py");
      source.Execute(scope);
      //Console.WriteLine(scope.GetVariable("cu"));
    }


    internal void OnStickerClick(MouseButtonEventArgs e)
    {
      // If results are showing, close all stickers
    }


    protected void OnPropertyChanged(string propertyName)
    {
      if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }

    public void NewStickerThreadSafeDelegate()
    {
      Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
        new Action(() => NewSticker()));
    }
  }

  public delegate void OnButtonClick(object sender, RoutedEventArgs args);

  [ValueConversion(typeof (DateTime), typeof (String))]
  public class DateTimeToStringConverter : IValueConverter
  {
    #region IValueConverter Members

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var dt = (DateTime) value;
      var ts = new TimeSpan();
      String ret = "";
      ts = DateTime.Now.Subtract(dt);
      if (ts.Days < 1)
      {
        if (ts.Hours > 0)
        {
          ret = ts.Hours + "h " + ts.Minutes + "mins ago";
        }
        else
        {
          ret = ts.Minutes + " minutes ago";
        }
      }
      else if (ts.Days == 1)
      {
        ret = "Yesterday, " + dt.ToShortTimeString();
      }
      else if (ts.Days > 1 && ts.Days < 7)
      {
        ret = ts.Days + " days ago!";
      }
      else if (ts.Days > 7)
      {
        ret = "more than a week ago!";
      }
      return ret;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }

    #endregion
  }
}