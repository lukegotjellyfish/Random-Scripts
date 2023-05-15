namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var targetFolder = GetInput("Enter target folder path: ");
            var chunkSize = int.Parse(GetInput("Enter number of files for each folder: "));

            await MainAsync(targetFolder, chunkSize);
        }

        static async Task MainAsync(string targetFolder, int chunkSize)
        {
            await RenameFoldersAsync(targetFolder);
            await CreateNumberedSubdirsAsync(targetFolder, chunkSize);
            await DeleteEmptyFoldersAsync(targetFolder);
        }

        static async Task RenameFoldersAsync(string targetFolder)
        {
            var random = new Random();

            foreach (var dirPath in Directory.GetDirectories(targetFolder))
            {
                if (!int.TryParse(Path.GetFileName(dirPath), out _))
                {
                    continue;
                }

                var success = false;
                while (!success)
                {
                    var newDirName = $"{Path.GetFileName(dirPath)}_{GenerateRandomHash()}";
                    var newDirPath = Path.Combine(targetFolder, newDirName);

                    try
                    {
                        Directory.Move(dirPath, newDirPath);
                        success = true;
                    }
                    catch (IOException)
                    {
                        // If the new folder name already exists, wait for a random time before trying again
                        await Task.Delay(random.Next(100, 500));
                    }
                }
            }
        }

        static async Task CreateNumberedSubdirsAsync(string targetFolder, int chunkSize)
        {
            var filePaths = Directory.GetFiles(targetFolder, "*", SearchOption.AllDirectories)
                .Where(p => !Directory.Exists(p))
                .OrderBy(p => File.GetLastWriteTime(p))
                .ToList();

            var totalFiles = filePaths.Count;
            var numChunks = (totalFiles + chunkSize - 1) / chunkSize;

            for (var i = 0; i < numChunks; i++)
            {
                var chunkFolder = Path.Combine(targetFolder, $"{i + 1}");
                Directory.CreateDirectory(chunkFolder);

                var chunkFilePaths = filePaths.Skip(i * chunkSize).Take(chunkSize);

                foreach (var filePath in chunkFilePaths)
                {
                    var fileName = Path.GetFileName(filePath);
                    var destPath = Path.Combine(chunkFolder, fileName);

                    await Task.Run(() => File.Move(filePath, destPath));
                }
            }
        }

        static async Task DeleteEmptyFoldersAsync(string targetFolder)
        {
            foreach (var dirPath in Directory.GetDirectories(targetFolder))
            {
                await DeleteEmptyFoldersAsync(dirPath);
            }

            foreach (var dirPath in Directory.GetDirectories(targetFolder))
            {
                if (!Directory.EnumerateFileSystemEntries(dirPath).Any())
                {
                    await Task.Run(() => Directory.Delete(dirPath));
                }
            }
        }

        static string GenerateRandomHash(int length = 15)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        static string GetInput(string message)
        {
            Console.Write(message);
            return Console.ReadLine();
        }
    }
}