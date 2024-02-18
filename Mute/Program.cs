using System.Runtime.InteropServices;
using System.Text;
using Spectre.Console.Cli;

namespace Mute
{
  class Program
  {
    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern short GetKeyState(int nVirtKey);

    static void Main(string[] args)
    {
      Console.InputEncoding = Encoding.Unicode;
      Console.OutputEncoding = Encoding.Unicode;

      Console.Clear();

      TUI.PlayerMods playerMode = TUI.PlayerMods.ALL;
      long pageNumber = 0;
      bool repeatMode = false;

      TUI.ShowTUI(playerMode, ref pageNumber);
      Task.Run(() => AutoUpdateTUI(ref playerMode, ref pageNumber));
      Task.Run(() => CheckHotkeys(ref playerMode, ref pageNumber, ref repeatMode));
      // Task.Run(() => ShowTUI(TrackSpace.TrackList, playerMode, ref pageNumber));

      AsyncInput.Start();
      for (;;) 
      {
        Thread.Sleep(100);

        if (!Player.IsPause && Player.IsPlayBackEnd)
        {
          if (!repeatMode)
            TrackSpace.NextTrack(playerMode);
          Player.PlayTrack(TrackSpace.TrackList[TrackSpace.CurrentTrackIndex].Item1);
          TUI.ShowTUI(playerMode, ref pageNumber);
        }


        string command = AsyncInput.Input;
        if (command == "")
          continue;

        command = command[..^1];

        if (command.StartsWith("p"))
        {
          try
          {
            long trackIndex = Convert.ToInt64(command[1..]) - 1;
            pageNumber = 0;
            if (playerMode == TUI.PlayerMods.SEARCH)
              trackIndex = TrackSpace.SearchList[trackIndex];

            TrackSpace.CurrentTrackIndex = trackIndex;
            Player.PlayTrack(TrackSpace.TrackList[trackIndex].Item1);
          }
          catch (System.Exception)
          {
            Player.PauseOrResume();
          }
        }
        if (command == "R")
        {
          playerMode = TUI.PlayerMods.ALL;
          pageNumber = 0;

          TrackSpace.Update();
          // Task.Run(() => AutoUpdateTUI(ref playerMode, ref pageNumber));
          if (TrackSpace.CurrentTrackIndex == -1)
            Player.CloseFile();
        }
        if (command == "up" || command == ";")
        {
          --pageNumber;
        }
        if (command == "down" || command == ".")
        {
          ++pageNumber;
        }
        if (command == "shuffle")
        {
          TrackSpace.SortBy = TrackSpace.SortType.SHUFFLE;
        }
        if (command == "by alphabet")
        {
          TrackSpace.SortBy = TrackSpace.SortType.BY_ALPHABET;
        }
        if (command.StartsWith("/"))
        {
          command = command[1..];
          if (command == "")
          {
            playerMode = TUI.PlayerMods.ALL;
            TrackSpace.ClearSearchList();
          }
          else
          {
            playerMode = TUI.PlayerMods.SEARCH;
            TrackSpace.Find(command);
          }
        }
        if (command.StartsWith("cd "))
        {
          command = command[3..];
          playerMode = TUI.PlayerMods.ALL;
          TrackSpace.ChangeDirectory(command);
          // Task.Run(() => AutoUpdateTUI(ref playerMode, ref pageNumber));
        }
        if (command.StartsWith("dl "))
        {
          command = command[3..];
          if (command.StartsWith("url="))
          {
            command = command[4..];
            Task.Run(() => DownloadManager.Download(command));
          }
          else
          {
            Task.Run(() => DownloadManager.GetInfo(command));
            playerMode = TUI.PlayerMods.DOWNLOAD;
          }
        }
        if (command.StartsWith("rn "))
        {
          command = command[3..];

          long trackIndex = Convert.ToInt32(command[..command.IndexOf(' ')]) - 1;
          string newName = command[(command.IndexOf(' ') + 1)..];

          double timePos = Player.Time;
          bool isTrackIndexCurrent = false;
          bool isPause = Player.IsPause;

          switch (playerMode)
          {
          case TUI.PlayerMods.ALL:
            if (TrackSpace.CurrentTrackIndex == trackIndex)
            {
              Player.CloseFile();
              isTrackIndexCurrent = true;
            }
            break;
          case TUI.PlayerMods.SEARCH:
            if (TrackSpace.CurrentTrackIndex == TrackSpace.SearchList[trackIndex])
            {
              Player.CloseFile();
              isTrackIndexCurrent = true;
            }

            trackIndex = TrackSpace.SearchList[trackIndex];
            break;
          case TUI.PlayerMods.QUEUE:
            if (TrackSpace.CurrentTrackIndex == TrackSpace.QueueList.ElementAt(Convert.ToInt32(trackIndex)))
            {
              Player.CloseFile();
              isTrackIndexCurrent = true;
            }

            trackIndex = TrackSpace.QueueList.ElementAt(Convert.ToInt32(trackIndex));
            break;
          default:
            break;
          }

          TrackSpace.Rename(trackIndex, newName);

          if (isTrackIndexCurrent)
          {
            Player.PlayTrack(TrackSpace.TrackList[trackIndex].Item1, timePos);

            if (isPause)
              Player.Pause();
          }
        }
        if (command.StartsWith("del "))
        {
          command = command[4..];

          Player.CloseFile();
          switch (playerMode)
          {
          case TUI.PlayerMods.ALL:
            TrackSpace.Delete(Convert.ToInt64(command.Split(' ').First()) - 1);
            break;
          case TUI.PlayerMods.SEARCH:
            TrackSpace.Delete(TrackSpace.SearchList[Convert.ToInt64(command.Split(' ').First()) - 1]);
            break;
          case TUI.PlayerMods.QUEUE:
            TrackSpace.Delete(TrackSpace.QueueList.ElementAt(Convert.ToInt32(command.Split(' ').First()) - 1));
            break;
          default:
            break;
          }
          TrackSpace.CurrentTrackIndex = TrackSpace.CurrentTrackIndex;

          Player.PlayTrack(TrackSpace.TrackList[TrackSpace.CurrentTrackIndex].Item1);
        }
        if (command.StartsWith("st "))
        {
          command = command[3..];

          double timeInSeconds = 0.0;

          if (command.EndsWith("%"))
          {
            timeInSeconds = Player.GetCurrentTrackDuration() * Convert.ToDouble(command[..^1]) / 100.0;
          }
          else
          {
            var HMS = command.Split(':').ToArray();

            for (int i = 1; i <= HMS.Length; ++i)
            {
              if (i == 1)
                timeInSeconds += Convert.ToDouble(HMS[HMS.Length - i]);
              else if (i == 2)
                timeInSeconds += Convert.ToDouble(HMS[HMS.Length - i]) * 60.0;
              else if (i == 3)
                timeInSeconds += Convert.ToDouble(HMS[HMS.Length - i]) * 60.0 * 60.0;
            }
          }

          Player.Time = timeInSeconds;
        }
        if (command == "exit")
        {
          Console.Clear();
          Player.CloseFile();
          return;
        }
        if (playerMode == TUI.PlayerMods.DOWNLOAD)
        {
          if (command != "back")
          {
            long dlTrackIndex = Convert.ToInt64(command) - 1;
            Task.Run(() => DownloadManager.DownloadByIndex(dlTrackIndex));
          }

          playerMode = TUI.PlayerMods.ALL;
        }
        
        AsyncInput.Flush();

        TUI.ShowTUI(playerMode, ref pageNumber);
        GC.Collect();
      }
    }

    static void AutoUpdateTUI(ref TUI.PlayerMods playerMode, ref long pageNumber)
    {
      //while (!TrackSpace.IsScanEnd)
      for (;;)
      {
        Thread.Sleep(1000);
        if (!Player.IsPlayBackEnd || !TrackSpace.IsScanEnd)
          TUI.ShowTUI(playerMode, ref pageNumber, Console.GetCursorPosition().Left, Console.GetCursorPosition().Top, false);
      }
    }

    static void CheckHotkeys(ref TUI.PlayerMods playerMode, ref long pageNumber, ref bool repeatMode)
    {
      for (;;)
      {
        if (GetKeyState(0x13) < 0) // Pause
        {
          Player.PauseOrResume();
          TUI.ShowTUI(playerMode, ref pageNumber, Console.GetCursorPosition().Left, Console.GetCursorPosition().Top, false);
        }
        if (GetKeyState(0xA5) < 0 && GetKeyState('R') < 0) // RAlt + R
        {
          repeatMode = !repeatMode;
          TUI.ShowTUI(playerMode, ref pageNumber, Console.GetCursorPosition().Left, Console.GetCursorPosition().Top, false);
        }
        if (GetKeyState(0xA5) < 0 && GetKeyState(0x25) < 0) // RAlt + Left
        {
          TrackSpace.PreviousTrack(playerMode);
          Player.PlayTrack(TrackSpace.TrackList[TrackSpace.CurrentTrackIndex].Item1);
          TUI.ShowTUI(playerMode, ref pageNumber, Console.GetCursorPosition().Left, Console.GetCursorPosition().Top, false);
        }
        if (GetKeyState(0xA5) < 0 && GetKeyState(0x27) < 0) // RAlt + Right
        {
          TrackSpace.NextTrack(playerMode);
          Player.PlayTrack(TrackSpace.TrackList[TrackSpace.CurrentTrackIndex].Item1);
          TUI.ShowTUI(playerMode, ref pageNumber, Console.GetCursorPosition().Left, Console.GetCursorPosition().Top, false);
        }
        if (GetKeyState(0xA5) < 0 && GetKeyState(0xA1) < 0 && GetKeyState(0x26) < 0) // RAlt + RShift + Up
        {
          Player.Volume += 1;
          TUI.ShowTUI(playerMode, ref pageNumber, Console.GetCursorPosition().Left, Console.GetCursorPosition().Top, false);
        }
        else if (GetKeyState(0xA5) < 0 && GetKeyState(0x26) < 0) // RAlt + Up
        {
          Player.Volume += 5;
          TUI.ShowTUI(playerMode, ref pageNumber, Console.GetCursorPosition().Left, Console.GetCursorPosition().Top, false);
        }
        if (GetKeyState(0xA5) < 0 && GetKeyState(0xA1) < 0 && GetKeyState(0x28) < 0) // RAlt + RShift + Down
        {
          Player.Volume -= 1;
          TUI.ShowTUI(playerMode, ref pageNumber, Console.GetCursorPosition().Left, Console.GetCursorPosition().Top, false);
        }
        else if (GetKeyState(0xA5) < 0 && GetKeyState(0x28) < 0) // RAlt + Down
        {
          Player.Volume -= 5;
          TUI.ShowTUI(playerMode, ref pageNumber, Console.GetCursorPosition().Left, Console.GetCursorPosition().Top, false);
        }
        if (GetKeyState(0xA1) < 0 && GetKeyState(0x25) < 0)
        {
          Player.Time -= 5;
          TUI.ShowTUI(playerMode, ref pageNumber, Console.GetCursorPosition().Left, Console.GetCursorPosition().Top, false);
        }
        if (GetKeyState(0xA1) < 0 && GetKeyState(0x27) < 0)
        {
          Player.Time += 5;
          TUI.ShowTUI(playerMode, ref pageNumber, Console.GetCursorPosition().Left, Console.GetCursorPosition().Top, false);
        }

        Thread.Sleep(100);
      }
    }
  }
}
