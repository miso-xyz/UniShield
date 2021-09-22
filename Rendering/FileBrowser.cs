using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace UniShield.Rendering
{
    class ListedFile
    {
        public ListedFile(string Path, string Name, FileType filetype)
        {
            path = Path;
            name = Name;
            fileType = filetype;
        }

        public enum FileType
        {
            File,
            Folder
        }

        public string name;
        public string path;
        public FileType fileType;
    }

    class FileBrowser
    {
        public int currentHighlighted = 0;
        public int defaultExtFilter = 0;
        public string title;
        private string currentPath;
        public List<ListedFile> fileList = new List<ListedFile>();
        public List<string> filterExt = new List<string>() { "*.*", "*.dat"};

        public void ListFiles(string path)
        {
            currentPath = path;
            fileList.Clear();
            DirectoryInfo dir = new DirectoryInfo(path);
            foreach (DirectoryInfo dir_ in dir.GetDirectories()) { fileList.Add(new ListedFile(dir_.FullName, dir_.Name, ListedFile.FileType.Folder)); }
            foreach (FileInfo file_ in dir.GetFiles()) { fileList.Add(new ListedFile(file_.FullName, file_.Name, ListedFile.FileType.File)); }
        }

        public void ListDrives()
        {
            currentPath = "DriveListing";
            fileList.Clear();
            foreach (DriveInfo drInfo in DriveInfo.GetDrives()) { if (!drInfo.IsReady) { continue; } fileList.Add(new ListedFile(drInfo.RootDirectory.FullName, "[" + drInfo.Name + "]: " + drInfo.VolumeLabel, ListedFile.FileType.Folder)); }
        }

        private void Unload()
        {
            for (int y = 0; y < Console.WindowHeight - 1; y++)
            {
                Console.CursorTop = y;
                fillPadding();
            }
        }

        private void fillPadding()
        {
            for (int x = Console.CursorLeft; x < 45; x++) { Console.Write(" "); }
            Console.CursorLeft = 0;
        }

        public static void ShowTutorial()
        {
            Program.AddToLog("  Navigating the File Explorer:", ConsoleColor.Cyan);
            Program.AddToLog("");
            Program.AddToLog("       Up/Down  = Select File in current directory ", ConsoleColor.Green);
            Program.AddToLog("         Enter  = Open directory/Validate Selection", ConsoleColor.Yellow);
            Program.AddToLog("     Backspace  = Go to parent directory", ConsoleColor.Red);
            Program.AddToLog("        Escape  = Cancel Operation", ConsoleColor.DarkRed);
            Program.AddToLog("            F1  = Goes to Drive Listing", ConsoleColor.DarkYellow);
            Program.AddToLog("            F2  = Goes to Application Folder", ConsoleColor.DarkGreen);
            Program.AddToLog("            F3  = Goes to Target Application Folder", ConsoleColor.DarkGray);
            Program.AddToLog("            F5  = Refresh Directory", ConsoleColor.Magenta);
            Program.AddToLog("         F6/F7  = Change Extension Filter", ConsoleColor.DarkCyan);
        }

        public void FilterFiles(List<ListedFile> src, out List<ListedFile> output, string filter)
        {
            List<ListedFile> files = new List<ListedFile>();
            foreach (ListedFile file in src)
            {
                if (file.fileType == ListedFile.FileType.File) { if (filter != "*.*") { if (Path.GetExtension(file.path) != filter.Replace("*", null)) { continue; } } }
                files.Add(file);
            }
            output = files;
        }

        public string Initialize()
        {
            Unload();
            int offset = 0, currentHighlightedPos = 0, currentExtFilter = defaultExtFilter;
            List<ListedFile> visibleFiles = new List<ListedFile>();
            FilterFiles(fileList, out visibleFiles, filterExt[currentExtFilter]);
            while (true)
            {
                int yPos = Console.CursorTop;
                Console.SetCursorPosition(0, 0);
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.DarkCyan;
                string filterText = "All Files";
                if (filterExt[currentExtFilter] != "*.*") { filterText = filterExt[currentExtFilter]; }
                Console.Write(" " + title + " - ");
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Cyan;
                Console.Write(" " + filterText + " ");
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.DarkCyan;
                fillPadding();
                Console.CursorTop += 2;
                for (int x = 0; x < visibleFiles.Count(); x++)
                {
                    Console.CursorLeft = 1;
                    ListedFile file = visibleFiles[x + offset];
                    if (x + offset == currentHighlighted)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.BackgroundColor = ConsoleColor.DarkCyan;
                        currentHighlightedPos = Console.CursorTop;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.BackgroundColor = ConsoleColor.Black;
                    }
                    string text = file.name;
                    if (text.Length > 42) { text = text.Substring(0,39) + "..."; }
                    Console.Write(" " + text + " ");
                    if (Console.CursorTop == Console.WindowHeight - 2) { break; }
                    Console.CursorTop++;
                    Console.CursorLeft = 0;
                }
                Console.SetCursorPosition(44, 0);
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Black;
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.DownArrow:
                        if (currentHighlighted < visibleFiles.Count() - 1) { currentHighlighted++; if (currentHighlightedPos == Console.WindowHeight - 2) { offset++; Unload(); } }
                        break;
                    case ConsoleKey.UpArrow:
                        try { visibleFiles[currentHighlighted - 1].path = visibleFiles[currentHighlighted - 1].path; }
                        catch { break; }
                        currentHighlighted--;
                        if (currentHighlightedPos == 2) { offset--; Unload(); }
                        break;
                    case ConsoleKey.Backspace:
                        currentHighlighted = 0;
                        currentHighlightedPos = 0;
                        offset = 0;
                        if (currentPath == Directory.GetDirectoryRoot(currentPath)) { ListDrives(); }
                        else { currentPath = Directory.GetParent(currentPath).FullName; ListFiles(currentPath);}
                        FilterFiles(fileList, out visibleFiles, filterExt[currentExtFilter]);
                        Unload();
                        break;
                    case ConsoleKey.Escape:
                        Console.CursorLeft -= 2; // fixes tiny graphical glitch
                        Console.Write(" ");      // fixes tiny graphical glitch
                        Unload();
                        return null;
                    case ConsoleKey.F5:
                        if (currentPath == "DriveListing") { ListDrives(); }
                        else { ListFiles(currentPath); }
                        FilterFiles(fileList, out visibleFiles, filterExt[currentExtFilter]);
                        Unload();
                        break;
                    case ConsoleKey.F1:
                        currentHighlighted = 0;
                        currentHighlightedPos = 0;
                        offset = 0;
                        ListDrives();
                        FilterFiles(fileList, out visibleFiles, filterExt[currentExtFilter]);
                        Unload();
                        break;
                    case ConsoleKey.F2:
                        currentHighlighted = 0;
                        currentHighlightedPos = 0;
                        offset = 0;
                        ListFiles(Environment.CurrentDirectory);
                        FilterFiles(fileList, out visibleFiles, filterExt[currentExtFilter]);
                        Unload();
                        break;
                    case ConsoleKey.F3:
                        currentHighlighted = 0;
                        currentHighlightedPos = 0;
                        offset = 0;
                        ListFiles(Directory.GetParent(Path.GetFullPath(Program.path)).FullName);
                        FilterFiles(fileList, out visibleFiles, filterExt[currentExtFilter]);
                        Unload();
                        break;
                    case ConsoleKey.F6:
                        currentHighlighted = 0;
                        currentHighlightedPos = 0;
                        offset = 0;
                        if (filterExt.Count() != 0) { if (currentExtFilter != 0) { currentExtFilter--; Unload(); } }
                        FilterFiles(fileList, out visibleFiles, filterExt[currentExtFilter]);
                        break;
                    case ConsoleKey.F7:
                        currentHighlighted = 0;
                        currentHighlightedPos = 0;
                        offset = 0;
                        if (filterExt.Count() != 0) { if (currentExtFilter != filterExt.Count() - 1) { currentExtFilter++; Unload(); } }
                        FilterFiles(fileList, out visibleFiles, filterExt[currentExtFilter]);
                        break;
                    case ConsoleKey.Enter:
                        if (visibleFiles[currentHighlighted].fileType == ListedFile.FileType.Folder)
                        {
                            currentPath = visibleFiles[currentHighlighted].path;
                            ListFiles(currentPath);
                            FilterFiles(fileList, out visibleFiles, filterExt[currentExtFilter]);
                            currentHighlighted = 0;
                            Unload();
                            break;
                        }
                        Unload();
                        return visibleFiles[currentHighlighted + offset].path;
                }
            }
        }
    }
}
