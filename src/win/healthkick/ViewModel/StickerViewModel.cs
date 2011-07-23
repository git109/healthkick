using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Shapes;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace HealthKick.ViewModel
{

  public class StickerViewModel
  {

    public Timer Timer;
    public StickerState State;
    public DAO Dao;
    public User User;
    public Stack<MainWindow> Windows = new Stack<MainWindow>();
    public Notifier notifier;
    private DateTime lastUpdate;

    public StickerViewModel(DAO dao)
    {
      this.Dao = dao;
      User = dao.GetUser();
      State = new StickerState();
      SetStatus("waiting");

      // start checking for status updates
      //Timer = new Timer(new TimerCallback(Tick), null, 0, 1000);

      notifier = new Notifier();
      notifier.DataContext = State;

      UpdateSticker();

    }

    public void UpdateSticker()
    {
      State.LastReading = DateTime.Now;
      //State.LastReading = User.LastReading;
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
      var ts = DateTime.Now.Subtract(State.LastReading);
      var timeSinceLastUpdate = DateTime.Now.Subtract(lastUpdate);

      // Do we want to update the 'time since last reading' caption
      if (timeSinceLastUpdate > TimeSpan.FromMinutes(5))
      {
        UpdateSticker();
        lastUpdate = DateTime.Now;
      }
      
      //if (ts.Hours > 6)
      //Console.WriteLine(ts.Seconds);
      if (ts.TotalSeconds > 10)
      {
        //SetStatus("waiting");
      }
      else
      {
        //SetStatus("usb_inserted");
      }
    }

    public void SetStatus(string s)
    {
      State.StateView = s;
      State.ShowButton = false;
      switch (s)
      {
        case "waiting":
          State.Msg = "waiting for blood glucose reading";
          State.Color = "#FFCC00";
          State.SubText = true;
          State.ShowSpinner = false;
          break;
        case "usb_inserted":
          State.Msg = "reading results";
          State.Color = "Yellow";
          State.SubText = false;
          State.ShowSpinner = true;
          break;
        case "good_results":
          // within 4-6 range
          State.Msg = "4.6";
          State.Status = "perfect!";
          State.SubText = true;
          State.Color = "#66CC00";
          State.ShowSpinner = false;
          // show smiley face
          break;
        case "normal_results":
          State.Msg = "reading complete";
          State.Color = "Dark Green";
          State.ShowSpinner = false;
          break;
        case "bad_results":
          // above 20
          State.Msg = "reading complete";
          State.Color = "Red";
          State.ShowSpinner = false;
          break;
        case "waiting_for_dose":
          State.Msg = "click OK when you have dosed";
          State.ShowButton = true;
          State.SubText = false;
          State.ShowSpinner = false;
          break;
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
      if (Windows.Count < System.Windows.SystemParameters.PrimaryScreenHeight / 250)
      {
        if (Windows.Count % 2 == 0)
        {
          mw.Left = System.Windows.SystemParameters.PrimaryScreenWidth - rand.Next(220, 300);
        }
        else
        {
          mw.Left = System.Windows.SystemParameters.PrimaryScreenWidth - rand.Next(300, 350);
        }
        mw.Top = System.Windows.SystemParameters.PrimaryScreenHeight - 300 - (Windows.Count * 150);
      }
      else
      {
        mw.Left = System.Windows.SystemParameters.PrimaryScreenWidth / 2 - rand.Next(200, 400);
        mw.Top = System.Windows.SystemParameters.PrimaryScreenHeight / 2 - rand.Next(200, 400);
      }
      Windows.Push(mw);
    }

    public class StickerState : INotifyPropertyChanged
    {
      private string _msg;
      public string Msg
      {
        get { return _msg; }
        set
        {
          _msg = value;
          OnPropertyChanged("Msg");
        }
      }

      private string _lastReading;
      public DateTime LastReading { get; set; }

      private string _status;
      public string Status
      {
        get { return _status; }
        set
        {
          _status = value;
          OnPropertyChanged("LastReading");
        }
      }

      private string _color;
      public string Color
      {
        get { return _color; }
        set
        {
          _color = value;
          OnPropertyChanged("Color");
        }
      }

      private bool _subText;
      public bool SubText
      {
        get { return _subText; }
        set
        {
          _subText = value;
          OnPropertyChanged("SubText");
        }
      }

      private bool _showButton;
      public bool ShowButton
      {
        get { return _showButton; }
        set
        {
          _showButton = value;
          OnPropertyChanged("ShowButton");
        }
      }

      private bool _showSpinner;
      public bool ShowSpinner
      {
        get { return _showSpinner; }
        set
        {
          _showSpinner = value;
          OnPropertyChanged("ShowSpinner");
        }
      }


      private string _stateView;
      public string StateView
      {
        get { return _stateView; }
        set
        {
          _stateView = value;
          OnPropertyChanged("StateView");
        }
      }

      public event PropertyChangedEventHandler PropertyChanged;

      protected void OnPropertyChanged(string propertyName)
      {
        if (this.PropertyChanged != null)
          PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }

    }

    public void Destroy(MainWindow window)
    {
      Console.WriteLine("Removed");
    }

    public void RunPythonCode()
    {
      ScriptRuntimeSetup setup = new ScriptRuntimeSetup();
      setup.LanguageSetups.Add(IronPython.Hosting.Python.CreateLanguageSetup(null));
      ScriptRuntime runtime = new ScriptRuntime(setup);
      runtime.IO.RedirectToConsole();
      ScriptEngine engine = runtime.GetEngine("IronPython");
      ScriptScope scope = engine.CreateScope();
      //ScriptSource source = engine.CreateScriptSourceFromString("print 'Hello World'\r\na=1", SourceCodeKind.Statements);
      ScriptSource source = engine.CreateScriptSourceFromFile("glucodump/main.py");
      source.Execute(scope);
      //Console.WriteLine(scope.GetVariable("cu"));
    }

  }

  [ValueConversion(typeof(DateTime), typeof(String))]
  public class DateTimeToStringConverter : IValueConverter
  {

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      DateTime dt = (DateTime) value;
      var ts = new TimeSpan();
      String ret = "";
      ts = DateTime.Now.Subtract(dt);
      if (dt.Day < 1)
      {
        ret = dt.ToShortTimeString();
      }
      else if (dt.Day == 1)
      {
        ret = "Yesterday, " + dt.ToShortTimeString();
      }
      else if (dt.Day > 1 && dt.Day < 7)
      {
        ret = dt.Day + " days ago!";
      }
      else if (dt.Day > 7)
      {
        ret = "more than a week ago!!!";
      }
      return ret;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
