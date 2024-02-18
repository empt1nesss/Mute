using System.Security.Cryptography.X509Certificates;
using System.Text;
using Spectre.Console;

// Перевести секунды в часы:минуты:секунды
// Учесть отсутствие треков
// Учесть смещение по огромному плейлисту
// Перекрасить фон выделенного трека

namespace Mute
{
  static public class TUI
  {
    public enum PlayerMods
    {
      ALL,
      SEARCH,
      QUEUE,
      DOWNLOAD
    }

    // public static void ConvertFromSecondsToTimeFormat(double seconds)
    public static string ConvertFromSecondsToTimeFormat(double seconds)
    {
      TimeSpan ts = TimeSpan.FromSeconds(seconds);
      string result = "";
      if (seconds == -1.0) return "~~:~~";
      if (Convert.ToBoolean(ts.Hours + ts.Days * 24))
      {
        if (Convert.ToString(ts.Hours).Length == 1)
          result += 0; result += ts.Hours + ":";
      }
      if (Convert.ToString(ts.Minutes).Length == 1) result += 0; result += ts.Minutes + ":";
      if (Convert.ToString(ts.Seconds).Length == 1) result += 0; result += ts.Seconds;
      return result;
    }

    // public static long GetPageNumber(long pageNumber, int tracklistLength, rowsToUse)
    // {
    //     long currentPage = 0;
    //     if (TrackSpace.CurrentTrackIndex == -1)
    //     {
    //         if (pageNumber < 0) pageNumber = 0;
    //         else if (pageNumber > Convert.ToInt32(TrackSpace.TrackList.Length / rowsToUse)) pageNumber = TrackSpace.TrackList.Length / rowsToUse;
    //     }
    //     else
    //     {
    //         currentPage = TrackSpace.CurrentTrackIndex / rowsToUse;
    //         if (pageNumber < 0)
    //         {
    //             if(currentPage + pageNumber < 0) { pageNumber = 0; }
    //             else { pageNumber = currentPage + pageNumber; }
    //         }
    //         else if (pageNumber > 0)
    //         {
    //             if(currentPage + pageNumber > Convert.ToInt32(TrackSpace.TrackList.Length / rowsToUse))
    //             {
    //                 pageNumber = (TrackSpace.TrackList.Length - 1) / rowsToUse;
    //             }
    //             else { pageNumber = currentPage + pageNumber; }
    //         }
    //     }
    // }

    public static long GetPageNumber(ref long pageNumber, int tracklistLength, int rowsToUse)
    {
      long currentPage = 0;
      if (TrackSpace.CurrentTrackIndex == -1)
      {
        if (pageNumber < 0) { pageNumber = 0; return 0; }
        else if (pageNumber > Convert.ToInt32(tracklistLength / rowsToUse)) return tracklistLength / rowsToUse;
        return pageNumber;
      }
      else
      {
        currentPage = TrackSpace.CurrentTrackIndex / rowsToUse;
        if (pageNumber < 0)
        {
          if (currentPage + pageNumber < 0) { pageNumber = 0; return 0; }
          else { return currentPage + pageNumber; }
        }
        else if (pageNumber > 0)
        {
          if (currentPage + pageNumber > Convert.ToInt32(tracklistLength / rowsToUse))
          {
            pageNumber = (tracklistLength - 1) / rowsToUse - currentPage; return (tracklistLength - 1) / rowsToUse;
          }
          else { return currentPage + pageNumber; }
        }
      }
      return currentPage;
    }

    public static string FormatTrackName(long trackIndex)
    {
      string trackName = TrackSpace.TrackList[trackIndex].Item1[..TrackSpace.TrackList[trackIndex].Item1.LastIndexOf('.')].Split("\\").Last();

      if (trackName.Length >= Console.WindowWidth - ConvertFromSecondsToTimeFormat(TrackSpace.TrackList[trackIndex].Item2).Length - 15)
        trackName = trackName[..(Console.WindowWidth - ConvertFromSecondsToTimeFormat(TrackSpace.TrackList[trackIndex].Item2).Length - 15)] + "... ";
      else
        trackName += new string(' ', Console.WindowWidth - trackName.Length - ConvertFromSecondsToTimeFormat(TrackSpace.TrackList[trackIndex].Item2).Length - 15);

      return trackName;
    }
    public static string FormatTrackNameForFilledRender(long trackIndex)
    {
      return TrackSpace.TrackList[trackIndex].Item1[..TrackSpace.TrackList[trackIndex].Item1.LastIndexOf('.')].Split("\\").Last();;
    }
    public static void ShowTUI(PlayerMods currentMode, ref long pageNumber, int oldCursorPosX = -1, int oldCursorPosY = -1, bool clearUserInput = true)
    {
      Tuple<string, double>[] TrackList = TrackSpace.TrackList;
      switch (currentMode)
      {
        case PlayerMods.ALL: { TrackList = TrackSpace.TrackList; break; }
        case PlayerMods.DOWNLOAD: { TrackList = DownloadManager.TrackList; break; }
      }
      // Tuple<string, double>[] TrackList = TrackSpace.TrackList;
      // if (currentMode == PlayerMods.DOWNLOAD) TrackList = TrackSpace.DownloadTrackList;
      Console.CursorVisible = false;
      // all, find, queue or download
      byte OFFSET = 3;
      int fieldHeight = Console.WindowHeight - OFFSET;

      int rowsToUse = 0;
      long localPageNumber = 0;
      switch (currentMode)
      {
        case PlayerMods.ALL:
          {
            rowsToUse = TrackList.Length >= fieldHeight ? fieldHeight : TrackList.Length;
            localPageNumber = GetPageNumber(ref pageNumber, TrackList.Length, rowsToUse);
            break;
          }
        case PlayerMods.SEARCH:
          {
            rowsToUse = TrackSpace.SearchList.Length >= fieldHeight ? fieldHeight : TrackSpace.SearchList.Length;
            localPageNumber = GetPageNumber(ref pageNumber, TrackSpace.SearchList.Length, rowsToUse);
            break;
          }
        case PlayerMods.QUEUE:
          {
            rowsToUse = TrackSpace.QueueList.Count >= fieldHeight ? fieldHeight : TrackSpace.QueueList.Count;
            localPageNumber = GetPageNumber(ref pageNumber, TrackSpace.QueueList.Count, rowsToUse);
            break;
          }
      }

      // // Console.Clear();
      var layoutInfo = new Grid();
      layoutInfo.AddColumn();
      layoutInfo.AddColumn();
      layoutInfo.AddColumn();
      layoutInfo.AddColumn();
      layoutInfo.AddColumn();

      long start = 0, end = 0;
      switch (currentMode)
      {
        case PlayerMods.ALL:
          {
            start = localPageNumber * rowsToUse;
            end = rowsToUse * localPageNumber + rowsToUse;
            break;
          }
        case PlayerMods.SEARCH:
          {
            start = 0;
            end = rowsToUse * localPageNumber + rowsToUse;
            break;
          }
        case PlayerMods.QUEUE:
          {
            start = 0;
            end = rowsToUse * localPageNumber + rowsToUse;
            break;
          }
        case PlayerMods.DOWNLOAD:
          {

            break;
          }
      }

      long indexToRender = -1;

      // for (long i = localPageNumber * rowsToUse; i < rowsToUse * localPageNumber + rowsToUse; i++)
      var endCursorPos = (0, 0);
      for (long i = start; i < end; i++)
      {
        if (i >= TrackList.Length) break;
        bool isStartsWithArrow = false;
        string resultTime = ConvertFromSecondsToTimeFormat(TrackList[i].Item2);

        switch (currentMode)
        {
          case PlayerMods.ALL:
            {
              isStartsWithArrow = TrackSpace.CurrentTrackIndex == i;
              if (isStartsWithArrow) indexToRender = i;
              layoutInfo.AddRow(new Text[]{
                            new Text(isStartsWithArrow ? " -> " : "    ", new Style(foreground: isStartsWithArrow ? ColorScheme.ALL_FILLED_ARROW_COLOR : ColorScheme.ALL_UNFILLED_ARROW_COLOR, background: isStartsWithArrow ? ColorScheme.ALL_FILLED_PRIMARY_BACKGROUND_COLOR : ColorScheme.ALL_UNFILLED_BACKGROUND_COLOR)).LeftJustified(),
                            new Text($"{i+1}.", new Style(foreground: isStartsWithArrow ? ColorScheme.ALL_FILLED_INDEX_COLOR : ColorScheme.ALL_UNFILLED_INDEX_COLOR, background: isStartsWithArrow ? ColorScheme.ALL_FILLED_SECONDARY_BACKGROUND_COLOR : ColorScheme.ALL_UNFILLED_BACKGROUND_COLOR)),
                            new Text(Convert.ToString(FormatTrackName(i)), new Style(foreground: isStartsWithArrow ? ColorScheme.ALL_FILLED_SONG_TITLE_COLOR : ColorScheme.ALL_UNFILLED_SONG_TITLE_COLOR, background: isStartsWithArrow ? ColorScheme.ALL_FILLED_PRIMARY_BACKGROUND_COLOR : ColorScheme.ALL_UNFILLED_BACKGROUND_COLOR)),
                            new Text(resultTime, new Style(foreground: isStartsWithArrow ? ColorScheme.ALL_FILLED_TIME_COLOR : ColorScheme.ALL_UNFILLED_TIME_COLOR, background: isStartsWithArrow ? ColorScheme.ALL_FILLED_SECONDARY_BACKGROUND_COLOR : ColorScheme.ALL_UNFILLED_BACKGROUND_COLOR)).RightJustified()
                        });
              Console.SetCursorPosition(0, fieldHeight + 1);
              AnsiConsole.Write(new string(' ', clearUserInput ? Console.WindowWidth : $"|CM| V{Convert.ToInt32(Player.Volume)}% ||:".Length), new Style(foreground: ColorScheme.ALL_INFO_TEXT_COLOR, background: ColorScheme.ALL_INFO_BACKGROUND_COLOR));
              Console.SetCursorPosition(0, fieldHeight + 1);
              // System.Console.WriteLine(oldCursorPosX);
              AnsiConsole.Write(new Text($"|CM| V{Convert.ToInt32(Player.Volume)}% ||:", new Style(foreground: ColorScheme.ALL_INFO_TEXT_COLOR, background: ColorScheme.ALL_INFO_BACKGROUND_COLOR)));

              endCursorPos = ($"|CM| V{Convert.ToInt32(Player.Volume)}% ||:".Length + 1, fieldHeight + 1);

              break;
            }
          case PlayerMods.SEARCH:
            {
              long localIndex = 0;
              if (TrackSpace.CurrentTrackIndex == TrackSpace.SearchList[i]) { localIndex = i; isStartsWithArrow = true; }
              layoutInfo.AddRow(new Text[]{
                            new Text(isStartsWithArrow ? " -> " : "    ", new Style(foreground: isStartsWithArrow ? ColorScheme.SEARCH_FILLED_ARROW_COLOR : ColorScheme.SEARCH_UNFILLED_ARROW_COLOR, background: isStartsWithArrow ? ColorScheme.SEARCH_FILLED_PRIMARY_BACKGROUND_COLOR : ColorScheme.SEARCH_UNFILLED_BACKGROUND_COLOR)).LeftJustified(),
                            new Text($"{i+1}.", new Style(foreground: isStartsWithArrow ? ColorScheme.SEARCH_FILLED_INDEX_COLOR : ColorScheme.SEARCH_UNFILLED_INDEX_COLOR, background: isStartsWithArrow ? ColorScheme.SEARCH_FILLED_SECONDARY_BACKGROUND_COLOR : ColorScheme.SEARCH_UNFILLED_BACKGROUND_COLOR)),
                            // new Text(Convert.ToString(TrackList[i].Item2), new Style(isStartsWithArrow ? ColorScheme.SEARCH_FILLED_SONG_TITLE_COLOR : ColorScheme.SEARCH_UNFILLED_SONG_TITLE_COLOR))
                            new Text(FormatTrackName(TrackSpace.SearchList[i]), new Style(foreground: isStartsWithArrow ? ColorScheme.SEARCH_FILLED_SONG_TITLE_COLOR : ColorScheme.SEARCH_UNFILLED_SONG_TITLE_COLOR, background: isStartsWithArrow ? ColorScheme.SEARCH_FILLED_PRIMARY_BACKGROUND_COLOR : ColorScheme.SEARCH_UNFILLED_BACKGROUND_COLOR)),
                            new Text(resultTime, new Style(foreground: isStartsWithArrow ? ColorScheme.SEARCH_FILLED_TIME_COLOR : ColorScheme.SEARCH_UNFILLED_TIME_COLOR, background: isStartsWithArrow ? ColorScheme.SEARCH_FILLED_SECONDARY_BACKGROUND_COLOR : ColorScheme.SEARCH_UNFILLED_BACKGROUND_COLOR)).RightJustified()
                        });
              Console.SetCursorPosition(0, fieldHeight + 1);
              AnsiConsole.Write(new string(' ', clearUserInput ? Console.WindowWidth : $"|CM| V{Convert.ToInt32(Player.Volume)}% ||:".Length), new Style(foreground: ColorScheme.SEARCH_INFO_TEXT_COLOR, background: ColorScheme.SEARCH_INFO_BACKGROUND_COLOR));
              Console.SetCursorPosition(0, fieldHeight + 1);
              AnsiConsole.Write(new Text($"|CM| V{Convert.ToInt32(Player.Volume)}% ||:", new Style(foreground: ColorScheme.SEARCH_INFO_TEXT_COLOR, background: ColorScheme.SEARCH_INFO_BACKGROUND_COLOR)));

              endCursorPos = ($"|CM| V{Convert.ToInt32(Player.Volume)}% ||:".Length + 1, fieldHeight + 1);

              break;
            }
          case PlayerMods.QUEUE:
            {
              long localIndex = 0;
              if (TrackSpace.CurrentTrackIndex != -1 && i == 0) { localIndex = 0; isStartsWithArrow = true; }
              layoutInfo.AddRow(new Text[]{
                            new Text(isStartsWithArrow ? " -> " : "    ", new Style(foreground: isStartsWithArrow ? ColorScheme.QUEUE_FILLED_ARROW_COLOR : ColorScheme.QUEUE_UNFILLED_ARROW_COLOR, background: isStartsWithArrow ? ColorScheme.QUEUE_FILLED_PRIMARY_BACKGROUND_COLOR : ColorScheme.QUEUE_UNFILLED_BACKGROUND_COLOR)).LeftJustified(),
                            new Text($"{i+1}.", new Style(foreground: isStartsWithArrow ? ColorScheme.QUEUE_FILLED_INDEX_COLOR : ColorScheme.QUEUE_UNFILLED_INDEX_COLOR, background: isStartsWithArrow ? ColorScheme.QUEUE_FILLED_SECONDARY_BACKGROUND_COLOR : ColorScheme.QUEUE_UNFILLED_BACKGROUND_COLOR)),
                            new Text(FormatTrackName(TrackSpace.QueueList.ElementAt(Convert.ToInt32(i))), new Style(foreground: isStartsWithArrow ? ColorScheme.QUEUE_FILLED_SONG_TITLE_COLOR : ColorScheme.QUEUE_UNFILLED_SONG_TITLE_COLOR, background: isStartsWithArrow ? ColorScheme.QUEUE_FILLED_PRIMARY_BACKGROUND_COLOR : ColorScheme.QUEUE_UNFILLED_BACKGROUND_COLOR)),
                            new Text(resultTime, new Style(foreground: isStartsWithArrow ? ColorScheme.QUEUE_FILLED_TIME_COLOR : ColorScheme.QUEUE_UNFILLED_TIME_COLOR, background: isStartsWithArrow ? ColorScheme.QUEUE_FILLED_SECONDARY_BACKGROUND_COLOR : ColorScheme.QUEUE_UNFILLED_BACKGROUND_COLOR)).RightJustified()
                        });
              Console.SetCursorPosition(0, fieldHeight + 1);
              AnsiConsole.Write(new string(' ', clearUserInput ? Console.WindowWidth : $"|CM| V{Convert.ToInt32(Player.Volume)}% ||:".Length), new Style(foreground: ColorScheme.QUEUE_INFO_TEXT_COLOR, background: ColorScheme.QUEUE_INFO_BACKGROUND_COLOR));
              Console.SetCursorPosition(0, fieldHeight + 1);
              AnsiConsole.Write(new Text($"|CM| V{Convert.ToInt32(Player.Volume)}% ||:", new Style(foreground: ColorScheme.QUEUE_INFO_TEXT_COLOR, background: ColorScheme.QUEUE_INFO_BACKGROUND_COLOR)));

              endCursorPos = ($"|CM| V{Convert.ToInt32(Player.Volume)}% ||:".Length + 1, fieldHeight + 1);

              break;
            }
          case PlayerMods.DOWNLOAD:
            {
              layoutInfo.AddRow(new Text[]{
                            new Text(isStartsWithArrow ? " -> " : "    ", new Style(foreground: isStartsWithArrow ? ColorScheme.DOWNLOAD_FILLED_ARROW_COLOR : ColorScheme.DOWNLOAD_UNFILLED_ARROW_COLOR, background: isStartsWithArrow ? ColorScheme.DOWNLOAD_FILLED_PRIMARY_BACKGROUND_COLOR : ColorScheme.DOWNLOAD_UNFILLED_BACKGROUND_COLOR)).LeftJustified(),
                            new Text($"{i+1}.", new Style(foreground: isStartsWithArrow ? ColorScheme.DOWNLOAD_FILLED_INDEX_COLOR : ColorScheme.DOWNLOAD_UNFILLED_INDEX_COLOR, background: isStartsWithArrow ? ColorScheme.DOWNLOAD_FILLED_SECONDARY_BACKGROUND_COLOR : ColorScheme.DOWNLOAD_UNFILLED_BACKGROUND_COLOR)),
                            // new Text(Convert.ToString(TrackList[i].Item2), new Style(foreground: isStartsWithArrow ? ColorScheme.DOWNLOAD_FILLED_SONG_TITLE_COLOR : ColorScheme.DOWNLOAD_UNFILLED_SONG_TITLE_COLOR, background: isStartsWithArrow ? ColorScheme.DOWNLOAD_FILLED_PRIMARY_BACKGROUND_COLOR : ColorScheme.DOWNLOAD_UNFILLED_BACKGROUND_COLOR)),
                            new Text(Convert.ToString(TrackList[i].Item2), new Style(foreground: isStartsWithArrow ? ColorScheme.DOWNLOAD_FILLED_SONG_TITLE_COLOR : ColorScheme.DOWNLOAD_UNFILLED_SONG_TITLE_COLOR, background: isStartsWithArrow ? ColorScheme.DOWNLOAD_FILLED_PRIMARY_BACKGROUND_COLOR : ColorScheme.DOWNLOAD_UNFILLED_BACKGROUND_COLOR)),
                            new Text(resultTime, new Style(foreground: isStartsWithArrow ? ColorScheme.DOWNLOAD_FILLED_TIME_COLOR : ColorScheme.DOWNLOAD_UNFILLED_TIME_COLOR, background: isStartsWithArrow ? ColorScheme.DOWNLOAD_FILLED_SECONDARY_BACKGROUND_COLOR : ColorScheme.DOWNLOAD_UNFILLED_BACKGROUND_COLOR)).RightJustified()
                        });
              Console.SetCursorPosition(0, fieldHeight + 1);
              AnsiConsole.Write(new string(' ', clearUserInput ? Console.WindowWidth : $"|CM| V{Convert.ToInt32(Player.Volume)}% ||:".Length), new Style(foreground: ColorScheme.DOWNLOAD_INFO_TEXT_COLOR, background: ColorScheme.DOWNLOAD_INFO_BACKGROUND_COLOR));
              Console.SetCursorPosition(0, fieldHeight + 1);
              AnsiConsole.Write(new Text($"|CM| V{Convert.ToInt32(Player.Volume)}% ||:", new Style(foreground: ColorScheme.DOWNLOAD_INFO_TEXT_COLOR, background: ColorScheme.DOWNLOAD_INFO_BACKGROUND_COLOR)));

              endCursorPos = ($"|CM| V{Convert.ToInt32(Player.Volume)}% ||:".Length + 1, fieldHeight + 1);

              break;
            }
        }
      }
      
      Console.SetCursorPosition(0, fieldHeight);
      System.Console.WriteLine(new string(' ', Console.WindowWidth));
    
      Console.SetCursorPosition(0, 0);
      AnsiConsole.Write(layoutInfo);

        if (layoutInfo.Rows.Count < rowsToUse)
        {
            for (int i = layoutInfo.Rows.Count - 1; i < rowsToUse; i++)
            {
                AnsiConsole.WriteLine(new string(' ', Console.WindowWidth));
            }
        }
        Console.SetCursorPosition(0, rowsToUse + 2);
        AnsiConsole.Write(new string(' ', Console.WindowWidth));

// Пофиксить пробелы с 14,500,1000,10000....
        int numberOfSpaces = Convert.ToString(indexToRender + 1).Length == 1 ? 3 : 2;
        if (indexToRender != -1) {
            switch(currentMode)
            {
                case PlayerMods.ALL:
                {
                    Console.SetCursorPosition(0, Convert.ToInt32(indexToRender - (localPageNumber * rowsToUse)));
                    Console.BackgroundColor = ColorScheme.ALL_FILLED_PRIMARY_BACKGROUND_COLOR;
                    Console.Write(new String(" ->   "));
                    Console.BackgroundColor = ColorScheme.ALL_FILLED_SECONDARY_BACKGROUND_COLOR;
                    Console.Write(new String($"{indexToRender + 1}."));
                    Console.BackgroundColor = ColorScheme.ALL_FILLED_PRIMARY_BACKGROUND_COLOR;
                    Console.Write(new String($"{new string(' ', numberOfSpaces)}{Convert.ToString(FormatTrackNameForFilledRender(indexToRender))}"));
                    Console.Write(" ", Convert.ToString(FormatTrackName(indexToRender)).Length);
                    Console.BackgroundColor = ColorScheme.ALL_UNFILLED_BACKGROUND_COLOR;
                    break;
                }
                case PlayerMods.SEARCH:
                {
                    Console.SetCursorPosition(0, Convert.ToInt32(indexToRender - (localPageNumber * rowsToUse)));
                    Console.BackgroundColor = ColorScheme.SEARCH_FILLED_PRIMARY_BACKGROUND_COLOR;
                    Console.Write(new String(" ->   "));
                    Console.BackgroundColor = ColorScheme.SEARCH_FILLED_SECONDARY_BACKGROUND_COLOR;
                    Console.Write(new String($"{indexToRender + 1}."));
                    Console.BackgroundColor = ColorScheme.SEARCH_FILLED_PRIMARY_BACKGROUND_COLOR;
                    Console.Write(new String($"{new string(' ', numberOfSpaces)}{Convert.ToString(FormatTrackNameForFilledRender(indexToRender))}"));
                    Console.Write(" ", Convert.ToString(FormatTrackName(indexToRender)).Length);
                    Console.BackgroundColor = ColorScheme.SEARCH_UNFILLED_BACKGROUND_COLOR;
                    break;
                }
                case PlayerMods.QUEUE:
                {
                    Console.SetCursorPosition(0, Convert.ToInt32(indexToRender - (localPageNumber * rowsToUse)));
                    Console.BackgroundColor = ColorScheme.QUEUE_FILLED_PRIMARY_BACKGROUND_COLOR;
                    Console.Write(new String(" ->   "));
                    Console.BackgroundColor = ColorScheme.QUEUE_FILLED_SECONDARY_BACKGROUND_COLOR;
                    Console.Write(new String($"{indexToRender + 1}."));
                    Console.BackgroundColor = ColorScheme.QUEUE_FILLED_PRIMARY_BACKGROUND_COLOR;
                    Console.Write(new String($"{new string(' ', numberOfSpaces)}{Convert.ToString(FormatTrackNameForFilledRender(indexToRender))}"));
                    Console.Write(" ", Convert.ToString(FormatTrackName(indexToRender)).Length);
                    Console.BackgroundColor = ColorScheme.QUEUE_UNFILLED_BACKGROUND_COLOR;
                    break;
                }
                case PlayerMods.DOWNLOAD:
                {
                    Console.SetCursorPosition(0, Convert.ToInt32(indexToRender - (localPageNumber * rowsToUse)));
                    Console.BackgroundColor = ColorScheme.DOWNLOAD_FILLED_PRIMARY_BACKGROUND_COLOR;
                    Console.Write(new String(" ->   "));
                    Console.BackgroundColor = ColorScheme.DOWNLOAD_FILLED_SECONDARY_BACKGROUND_COLOR;
                    Console.Write(new String($"{indexToRender + 1}."));
                    Console.BackgroundColor = ColorScheme.DOWNLOAD_FILLED_PRIMARY_BACKGROUND_COLOR;
                    Console.Write(new String($"{new string(' ', numberOfSpaces)}{Convert.ToString(FormatTrackNameForFilledRender(indexToRender))}"));
                    Console.Write(" ", Convert.ToString(FormatTrackName(indexToRender)).Length);
                    Console.BackgroundColor = ColorScheme.DOWNLOAD_UNFILLED_BACKGROUND_COLOR;
                    break;
                }
            }
        }

      int indexForDuration = 0;
      if (localPageNumber == Convert.ToInt32(TrackList.Length / rowsToUse)) indexForDuration = TrackList.Length % rowsToUse;
      else indexForDuration = rowsToUse;

      GC.Collect();

      if (oldCursorPosX >= 0 && oldCursorPosY >= 0)
        Console.SetCursorPosition(oldCursorPosX, oldCursorPosY);
      else
        Console.SetCursorPosition(endCursorPos.Item1, endCursorPos.Item2);

      Console.CursorVisible = true;
    }
  }
}