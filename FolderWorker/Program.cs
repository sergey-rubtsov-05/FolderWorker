using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FolderWorker
{
    class Program
    {
        private static bool _silentMode;
        private static string _mainPath;
        private static int _needFreeSpaceInGb;

        #region Params global variables

        const string SilentModeParam = "-silentMode";
        const string PathParam = "-path";
        const string FreeSpaceParam = "-freeSpace";

        #endregion

        const string ParamValueNotSpecifed = "Значение для параметра {0} не указано!";
        static void Main(string[] args)
        {
            if (args.Contains("-testmode"))
            {
                CreateForders(800);
                CreateEmptyFiles(new DirectoryInfo(_mainPath).GetDirectories());
                Console.ReadKey();
                Environment.Exit(0);
            }
            try
            {
                Console.WriteLine($@"
Программа для освобождения места путем удаления папок начиная со старейших по дате создания и их содержимого из папки указаной в параметре {PathParam} до тех пор пока свободное место не достигнет значения указаного в параметре {FreeSpaceParam} (по умолчанию 100 гб)
Пример ввода параметров:
FolderWorker.exe {PathParam} ""D:\SomeFolder"" {FreeSpaceParam} 300 {SilentModeParam}
    {PathParam}: обязательный параметр. Путь к папке из которой будут удаляться папки
    {FreeSpaceParam}: не обязательный параметр. Указывается в гигабайтах, будет удалять папки до тех пор, пока не достигнет этого количества свободного места на диске, может удалить все папки, в директории указаной в параметре {PathParam}.
    {SilentModeParam}: не обязательный параметр. Указывает останавливать ли программу при показе информационных сообщений");
                ReadArgs(args);
                DriveInfo driveInfo = GetCurrentDriveInfo(_mainPath);
                CheckFreeSpace(driveInfo);
                var sw = Stopwatch.StartNew();
                var orderedDirectories = new DirectoryInfo(_mainPath).EnumerateDirectories().OrderBy(dir => dir.CreationTime);
                var count = 0;
                long size = 0;
                foreach (var directory in orderedDirectories)
                {
                    var directorySize = GetDirectorySize(directory);
                    size += directorySize;
                    directory.Delete(true);
                    count++;
                    if (driveInfo.TotalFreeSpace >= (long)_needFreeSpaceInGb*1024*1024*1024)
                    {
                        break;
                    }
                }
                sw.Stop();
                Logger.Info($"Папок удалено: {count} шт.\nОбъем: {size} byte, {size / 1024} kb, {size / 1024 / 1024} mb");
                Console.WriteLine($"Программа отработала за {sw.ElapsedMilliseconds} ms, {sw.ElapsedMilliseconds/1000} sec");
            }
            catch (Exception e)
            {
                Logger.Error($"Произошла критическая ошибка: {e.Message}\nStackTrace:\n{e.StackTrace}", _silentMode);
            }
            if (!_silentMode)
                Console.Read();
        }

        private static DriveInfo GetCurrentDriveInfo(string previewPath)
        {
            var driveName = Path.GetPathRoot(previewPath);
            if (string.IsNullOrWhiteSpace(driveName))
            {
                Logger.Warning($"Не удалось из пути {_mainPath} извлесь букву диска!", _silentMode);
                return null;
            }
            var drive = new DriveInfo(driveName);
            if (!drive.IsReady)
            {
                Logger.Warning($"Диск {drive.Name} не готов к работе", _silentMode);
                return null;
            }
            return drive;
        }

        private static void CheckFreeSpace(DriveInfo drive)
        {
            if (drive.TotalFreeSpace >= (long)_needFreeSpaceInGb*1024*1024*1024)
            {
                Logger.Info($"Свободного места: {drive.TotalFreeSpace/1024/1024/1024} больше или равно {_needFreeSpaceInGb}\n" +
                            "Работа программы будет прекращена");
                if (!_silentMode)
                    Console.Read();
                Environment.Exit(0);
            }
        }

        private static void ReadArgs(string[] args)
        {
            ReadSilentModeParam(args);
            ReadPath(args);
            ReadNeedFreeSpace(args);
        }

        private static void ReadSilentModeParam(string[] args)
        {
            _silentMode = args.Contains(SilentModeParam);
        }

        private static void ReadNeedFreeSpace(string[] args)
        {
            var defaultValue = 100;
            if (!args.Contains(FreeSpaceParam))
            {
                Logger.Info($"Параметр {FreeSpaceParam} не указан!\nЗначение по умолчанию {defaultValue} гб.");
                _needFreeSpaceInGb = defaultValue;
                return;
            }
            var stringValue = GetParamValue(args, FreeSpaceParam);
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                Logger.Warning(string.Format(ParamValueNotSpecifed, FreeSpaceParam), _silentMode);
                return;
            }
            int value;
            if (!int.TryParse(stringValue, out value))
            {
                Logger.Warning($"Значением параметра {FreeSpaceParam} должно быть целое число!", _silentMode);
                return;
            }
            _needFreeSpaceInGb = value;
        }

        private static void ReadPath(string[] args)
        {
            if (!args.Contains(PathParam))
            {
                Logger.Warning($"Параметр {PathParam} не указан!", _silentMode);
            }
            var value = GetParamValue(args, PathParam);
            if (string.IsNullOrWhiteSpace(value))
            {
                Logger.Warning(string.Format(ParamValueNotSpecifed, PathParam), _silentMode);
                return;
            }
            if (!Directory.Exists(value))
            {
                Logger.Warning($"Указанная директория {value} не существует", _silentMode);
            }
            _mainPath = value;
        }

        private static string GetParamValue(string[] args, string paramName)
        {
            string value = null;
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.Equals(paramName))
                {
                    if (i + 1 >= args.Length)
                        break;

                    value = args[i + 1];
                    break;
                }
            }
            return value;
        }

        private static long GetDirectorySize(DirectoryInfo directory)
        {
            return directory.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        }

        private static void CreateEmptyFiles(DirectoryInfo[] dis)
        {
            var randomArray = GetRandomArray(dis.Length, 800);
            byte[] bytes = new byte[1024 * 1024];
            var sw = Stopwatch.StartNew();
            foreach (var num in randomArray)
            {
                var newFileName = Guid.NewGuid();
                File.WriteAllBytes(Path.Combine(dis[num].FullName, newFileName.ToString()), bytes);
            }
            sw.Stop();
            Console.WriteLine($"Операция создания файлов заняла: {sw.ElapsedMilliseconds} ms");
        }

        private static int[] GetRandomArray(int max, int count)
        {
            var resultList = new List<int>();
            if (max == count)
            {
                for (var i = 0; i < max; i++)
                {
                    resultList.Add(i);
                }
                return resultList.ToArray();
            }
            while (resultList.Count < count)
            {
                var number = new Random().Next(max);
                if (!resultList.Contains(number))
                {
                    resultList.Add(number);
                }
            }
            return resultList.ToArray();
        }

        private static void CreateForders(int foldersCount)
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < foldersCount; i++)
            {
                var newGuidName = Guid.NewGuid();
                var newPath = Path.Combine(_mainPath, newGuidName.ToString());
                Console.WriteLine(newPath);
                Directory.CreateDirectory(newPath);
            }
            sw.Stop();
            Console.WriteLine($"Операция создания директорий заняла: {sw.ElapsedMilliseconds}");
        }
    }
}
