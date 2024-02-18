using System.Collections;
using System.Collections.Specialized;
using System.Reflection.Metadata.Ecma335;
using NAudio.Wave;


namespace Mute
{
  static class TrackSpace
  {
    static private Random _random;


    public enum SortType
    {
      BY_ALPHABET,
      SHUFFLE
    }

    static public string MainDirectory { get; private set; }
    static public Tuple<string, double>[] TrackList { get; private set; }
    static public bool IsScanEnd { get; private set; }
    static public long[] SearchList { get; private set; }
    static private SortType _sort;
    static public SortType SortBy
    {
      get
      {
        return _sort;
      }
      set
      {
        string currentTrackName = CurrentTrack.Item1;

        switch (value)
        {
          case SortType.BY_ALPHABET:
            Array.Sort(TrackList, (x, y) => string.Compare(x.Item1, y.Item1));
            break;
          case SortType.SHUFFLE:
            _random.Shuffle(TrackList);
            break;
          default:
            break;
        }

        for (long i = 0; i < TrackList.Length && currentTrackName != ""; ++i)
          if (TrackList[i].Item1 == currentTrackName)
          {
            CurrentTrackIndex = i;
            break;
          }
      }
    }
    static public Queue<long> QueueList { get; private set; }
    static private long _currentTrackIndex;
    static public long CurrentTrackIndex
    {
      get
      {
        return _currentTrackIndex;
      }
      set
      {
        _currentTrackIndex = value;
        while (_currentTrackIndex < 0)
          _currentTrackIndex += TrackList.Length;
        while (_currentTrackIndex >= TrackList.Length)
          _currentTrackIndex -= TrackList.Length;
      }
    }
    static public long CurrentTrackIndexSL
    {
      get
      {
        for (long i = 0; i < SearchList.Length; ++i)
        {
          if (SearchList[i] == CurrentTrackIndex)
            return i;
        }
        return -1;
      }
    }
    static public Tuple<string, double> CurrentTrack
    {
      get
      {
        if (CurrentTrackIndex < 0)
          return new Tuple<string, double>("", 0.0);

        return TrackList[CurrentTrackIndex];
      }
    }

    static TrackSpace()
    {
      _random = new Random();
      ChangeDirectory("D:\\Coding\\C#\\CM\\test");
      // ChangeDirectory("C:\\aimp");
      
      // TODO:
      //  - Read main directory from config
      //  - if config doesn't exist create it
      //    and get main directory from user input

      
    }

    static public void NextTrack(TUI.PlayerMods playerMode)
    {
      switch (playerMode)
      {
      case TUI.PlayerMods.ALL:
        ++CurrentTrackIndex;
        break;
      case TUI.PlayerMods.SEARCH:
        long indexInSearchList = Array.IndexOf(SearchList, CurrentTrackIndex);
        if (indexInSearchList == -1)
          return;

        ++indexInSearchList;
        if (indexInSearchList >= SearchList.Length)
          indexInSearchList -= SearchList.Length;
        
        CurrentTrackIndex = SearchList[indexInSearchList];

        break;
      case TUI.PlayerMods.QUEUE:
        QueueList.Dequeue();
        CurrentTrackIndex = QueueList.Peek();
        break;
      case TUI.PlayerMods.DOWNLOAD:
        ++CurrentTrackIndex;
        break;
      }
    }
    static public void PreviousTrack(TUI.PlayerMods playerMode)
    {
      switch (playerMode)
      {
      case TUI.PlayerMods.ALL:
        --CurrentTrackIndex;
        break;
      case TUI.PlayerMods.SEARCH:
        long indexInSearchList = Array.IndexOf(SearchList, CurrentTrackIndex);
        if (indexInSearchList == -1)
          return;

        --indexInSearchList;
        if (indexInSearchList >= SearchList.Length)
          indexInSearchList -= SearchList.Length;
        
        CurrentTrackIndex = SearchList[indexInSearchList];

        break;
      case TUI.PlayerMods.QUEUE:
        break;
      case TUI.PlayerMods.DOWNLOAD:
        --CurrentTrackIndex;
        break;
      }
    }
    static public void Find(string request)
    {
      for (int i = 0; i < request.Length; ++i)
      {
        if (request[i] >= 'a' && request[i] <= 'z')
        {
          request = request.Insert(i, Convert.ToString((char)(request[i] + ('A' - 'a'))));
          request = request.Remove(i + 1, 1);
        }
        else if (request[i] >= 'a' && request[i] <= 'z')
        {
          request = request.Insert(i, Convert.ToString((char)(request[i] + ('А' - 'а'))));
          request = request.Remove(i + 1, 1);
        }
        else if (request[i] == 'Ё' || request[i] == 'ё')
        {
          request = request.Remove(i, 1);
          request = request.Insert(i, "Е");
        }
      }

      ClearSearchList();
      for (long i = 0; i < TrackList.Length; ++i)
      {
        string trackName = TrackList[i].Item1;
        trackName = trackName.Remove(0, trackName.LastIndexOf('\\') + 1);
        trackName = trackName.Remove(trackName.Length - 4, 4);

        for (int c = 0; c < trackName.Length; ++c)
        {
          if (trackName[c] >= 'a' && trackName[c] <= 'z')
          {
            trackName = trackName.Insert(c, Convert.ToString((char)(trackName[c] + ('A' - 'a'))));
            trackName = trackName.Remove(c + 1, 1);
          }
          else if (trackName[c] >= 'a' && trackName[c] <= 'z')
          {
            trackName = trackName.Insert(c, Convert.ToString((char)(trackName[c] + ('А' - 'а'))));
            trackName = trackName.Remove(c + 1, 1);
          }
          else if (trackName[c] == 'Ё' || trackName[c] == 'ё')
          {
            trackName = trackName.Remove(c, 1);
            trackName = trackName.Insert(c, "Е");
          }
        }

        if (trackName.Contains(request))
          SearchList = SearchList.Append(i).ToArray();
      }

      GC.Collect();
    }

    static public void ClearSearchList()
    {
      SearchList = [];
    }

    static public void ChangeDirectory(string directory)
    {
      IsScanEnd = false;

      MainDirectory = directory;

      string[] fileList = Directory.GetFiles(MainDirectory);
      TrackList = [];
      foreach (var fileName in fileList)
      {
        if (fileName.EndsWith(".mp3"))
          TrackList = TrackList.Append(new Tuple<string, double>(fileName, -1.0)).ToArray();
        else if (fileName.EndsWith(".wav"))
          TrackList = TrackList.Append(new Tuple<string, double>(fileName, -1.0)).ToArray();
      }

      Task.Factory.StartNew(() =>
      {
        for (;;)
        {
          long i = -1;
          for (long i2 = 0; i2 < TrackList.Length; ++i2)
            if (TrackList[i2].Item2 == -1.0)
            {
              i = i2;
              break;
            }

          if (i == -1)
            break;

          if (TrackList[i].Item1.EndsWith(".mp3"))
          {
            var tempReader = new Mp3FileReader(TrackList[i].Item1);
            TrackList[i] = new Tuple<string, double>
            (
              TrackList[i].Item1,
              tempReader.TotalTime.TotalSeconds
            );
            tempReader.Close();
          }
          else if (TrackList[i].Item1.EndsWith(".wav"))
          {
            var tempReader = new WaveFileReader(TrackList[i].Item1);
            TrackList[i] = new Tuple<string, double>
            (
              TrackList[i].Item1,
              tempReader.TotalTime.TotalSeconds
            );
            tempReader.Close();
          }
          GC.Collect();
          Thread.Sleep(10);
        }
        IsScanEnd = true;
      });

      SearchList = [];
      _sort = SortType.BY_ALPHABET;
      QueueList = new Queue<long>();
      _currentTrackIndex = -1;

      GC.Collect();
    }
    static public void Update()
    {
      IsScanEnd = false;

      string currentTrackName = "";
      if (CurrentTrackIndex != -1)
        currentTrackName = TrackList[CurrentTrackIndex].Item1;

      string[] fileList = Directory.GetFiles(MainDirectory);
      TrackList = [];
      foreach (var fileName in fileList)
      {
        if (fileName.EndsWith(".mp3"))
          TrackList = TrackList.Append(new Tuple<string, double>(fileName, -1.0)).ToArray();
        else if (fileName.EndsWith(".wav"))
          TrackList = TrackList.Append(new Tuple<string, double>(fileName, -1.0)).ToArray();
      }

      Task.Factory.StartNew(() =>
      {
        for (long i = 0; i < TrackList.Length; ++i)
        {
          if (TrackList[i].Item1.EndsWith(".mp3"))
          {
            var tempReader = new Mp3FileReader(TrackList[i].Item1);
            TrackList[i] = new Tuple<string, double>
            (
              TrackList[i].Item1,
              tempReader.TotalTime.TotalSeconds
            );
            tempReader.Close();
          }
          else if (TrackList[i].Item1.EndsWith(".wav"))
          {
            var tempReader = new WaveFileReader(TrackList[i].Item1);
            TrackList[i] = new Tuple<string, double>
            (
              TrackList[i].Item1,
              tempReader.TotalTime.TotalSeconds
            );
            tempReader.Close();
          }
          GC.Collect();
          Thread.Sleep(10);
        }
        IsScanEnd = true;
      });

      SearchList = [];
      _sort = SortType.BY_ALPHABET;
      QueueList = new Queue<long>();

      _currentTrackIndex = -1;
      for (long i = 0; i < TrackList.Length && (currentTrackName != ""); ++i)
        if (TrackList[i].Item1 == currentTrackName)
        {
          CurrentTrackIndex = i;
          break;
        }

      GC.Collect();
    }

    // ChangePlayList

    static public void Rename(long index, string newName)
    {
      if (index < 0 || index >= TrackList.Length)
        return;

      newName = MainDirectory + "\\" + newName +
                TrackList[index].Item1.Remove(0, TrackList[index].Item1.LastIndexOf('.'));
      
      try
      {
        File.Move(TrackList[index].Item1, newName);
      }
      catch (System.Exception)
      {
        return;
      }

      TrackList[index] = new Tuple<string, double>(newName, TrackList[index].Item2);
      GC.Collect();
    }

    static public void Delete(long index)
    {
      if (index < 0 || index >= TrackList.Length)
        return;

      File.Delete(TrackList[index].Item1);
      TrackList = TrackList.Where((v, i) => i != index).ToArray();
      GC.Collect();

      if (index >= TrackList.Length)
        index = TrackList.Length - 1;
    }

    static public void SetPos(long oldIndex, long newIndex)
    {
      if (oldIndex < 0 || oldIndex >= TrackList.Length ||
          newIndex < 0 || newIndex >= TrackList.Length ||
          oldIndex == newIndex)
        return;

      Tuple<string, double>[] temp = [];

      for (long i = 0; i < newIndex; ++i)
      {
        if (i == oldIndex)
          continue;
        temp = temp.Append(TrackList[i]).ToArray();
      }
      temp = temp.Append(TrackList[oldIndex]).ToArray();
      for (long i = newIndex + 1; i < TrackList.Length; ++i)
      {
        if (i == oldIndex)
          continue;
        temp = temp.Append(TrackList[i]).ToArray();
      }

      TrackList = temp;

      if (CurrentTrackIndex == oldIndex)
        CurrentTrackIndex = newIndex;
      else
      {
        if (oldIndex < newIndex)
          if (CurrentTrackIndex > oldIndex && CurrentTrackIndex <= newIndex)
            --CurrentTrackIndex;
        else
          if (CurrentTrackIndex >= newIndex && CurrentTrackIndex < oldIndex)
            ++CurrentTrackIndex;
      }

      GC.Collect();
    }

    static public void AddToQueue(long index)
    {
      if (index < 0 || index >= TrackList.Length)
        return;
      
      QueueList.Enqueue(index);
    }

    static public void RemoveFromQueue(long index)
    {
      if (index < 0 || index >= QueueList.Count)
        return;

      if (index == 0)
      {
        QueueList.Dequeue();
        if (QueueList.Count >= 1)
          CurrentTrackIndex = QueueList.Peek();
        else
          ++CurrentTrackIndex;
      }
      else
        QueueList = new Queue<long>(QueueList.Where(x => x != index));

      GC.Collect();
    }

    static public void ClearQueue()
    {
      if (QueueList.Count <= 0)
        return;

      QueueList = [];
      GC.Collect();
    }
  }
}
