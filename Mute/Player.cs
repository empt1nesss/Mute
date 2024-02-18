using NAudio.Wave;

namespace Mute
{
  static class Player
  {
    static private Mp3FileReader? Mp3Reader;
    static private WaveFileReader? WavReader;
    static private WaveOutEvent? AudioStream;

    
    static public string TrackFormat { get; private set; }
    static public bool IsPause { get; private set; }
    static public bool IsFileOpened { get; private set; }
    static public bool IsPlayBackEnd
    {
      get
      {
        if (!IsFileOpened)
          return false;

        return AudioStream.PlaybackState == PlaybackState.Stopped;
      }
    }
    static private float _volume;
    static public float Volume
    {
      get
      {
        return _volume * 100;
      }
      set
      {
        _volume = value / 100.0f;
        if (_volume > 1.0f)
          _volume = 1.0f;
        else if (_volume < 0.0f)
          _volume = 0.0f;

        if (IsFileOpened)
          AudioStream.Volume = _volume;
      }
    }

    static public double Time
    {
      get
      {
        if (!IsFileOpened)
          return 0.0;
        if (TrackFormat == "mp3")
          return Mp3Reader.CurrentTime.TotalSeconds;
        else if (TrackFormat == "wav")
          return WavReader.CurrentTime.TotalSeconds;
        
        return 0.0;
      }
      set
      {
        if (IsFileOpened)
        {
          if (value >= GetCurrentTrackDuration())
            CloseFile();
          else if (value < 0.0)
          {
            if (TrackFormat == "mp3")
              Mp3Reader.CurrentTime = TimeSpan.FromSeconds(0.0);
            else if (TrackFormat == "wav")
              WavReader.CurrentTime = TimeSpan.FromSeconds(0.0);
          }
          else
            if (TrackFormat == "mp3")
              Mp3Reader.CurrentTime = TimeSpan.FromSeconds(value);
            else if (TrackFormat == "wav")
              WavReader.CurrentTime = TimeSpan.FromSeconds(value);
        }
      }
    }

    static Player()
    {
      IsPause = true;
      IsFileOpened = false;
      TrackFormat = "";
      Volume = 15.0f;

      AudioStream = new WaveOutEvent();
    }

    static public void PlayTrack(string path, double timePos = 0.0)
    {
      CloseFile();
      if (path.EndsWith(".mp3"))
      {
        Mp3Reader = new Mp3FileReader(path);
        AudioStream.Init(Mp3Reader); 
        TrackFormat = "mp3";
      }
      else if (path.EndsWith(".wav"))
      {
        WavReader = new WaveFileReader(path);
        AudioStream.Init(WavReader);
        TrackFormat = "wav";
      }
      GC.Collect();

      IsFileOpened = true;
      
      AudioStream.Volume = Volume / 100.0f;
      Time = timePos;
      AudioStream.Play();

      IsPause = false;
    }

    static public void CloseFile()
    {
      if (IsFileOpened)
      {
        AudioStream.Stop();
        IsPause = true;

        if (TrackFormat == "mp3")
          Mp3Reader.Close();
        else if (TrackFormat == "wav")
          WavReader.Close();

        IsFileOpened = false;
      }
    }

    static public void PauseOrResume()
    {
      if (!IsFileOpened)
        return;

      if (IsPause)
      {
        AudioStream.Play();
        IsPause = false;
      }
      else
      {
        AudioStream.Pause();
        IsPause = true;
      }
    }

    static public void Pause()
    {
      if (!IsPause && IsFileOpened)
      {
        AudioStream.Pause();
        IsPause = true;
      }
    }

    static public void Resume()
    {
      if (IsPause && IsFileOpened)
      {
        AudioStream.Play();
        IsPause = false;
      }
    }

    static public double GetCurrentTrackDuration()
    {
      if (!IsFileOpened)
        return 0.0;
      
      if (TrackFormat == "mp3")
        return Mp3Reader.TotalTime.TotalSeconds;
      else if (TrackFormat == "wav")
        return WavReader.TotalTime.TotalSeconds;

      return 0.0;
    }
  }
}
