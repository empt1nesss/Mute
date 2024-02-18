static class AsyncInput
{
  static private string _input;
  static public string Input
  {
    get
    {
      return _input;
    }
  }
  static private bool suspend;

  static AsyncInput()
  {
    suspend = true;
    _input = "";

    Task.Run(() =>
    {
      for (;;)
      {
        if (!suspend)
          _input = Console.ReadLine() + "\n";

        Thread.Sleep(150);
      }
    });
  }

  static public void Flush()
  {
    _input = "";
  }

  // static public void Pause()
  // {
  //   suspend = true;
  // }

  // static public void Resume()
  // {
  //   _input = "";
  //   suspend = false;
  // }

  static public void Start()
  {
    suspend = false;
  }
}