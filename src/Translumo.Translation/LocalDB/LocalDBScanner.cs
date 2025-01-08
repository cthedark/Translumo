using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Translumo.Infrastructure.Constants;

namespace Translumo.Translation
{
    // Responsible for reading Local DBs
    public class LocalDBScanner
    {
        public static string ROOT_DIRECTORY_DISPLAY = "--root--";

        private readonly ILogger _logger;

        public LocalDBScanner(ILogger logger)
        {
            _logger = logger;
        }

        public List<LocalDB> ScanForLocalDBDirectories() {
            var list = new List<LocalDB>();
            // check if root has data files
            if (DirectoryHasDBFiles(Global.LocalDBPath)) list.Add(new LocalDB(ROOT_DIRECTORY_DISPLAY, _logger));

            var directories = Directory.GetDirectories(Global.LocalDBPath);
            foreach(string directory in directories) {
                if(DirectoryHasDBFiles(directory)) {
                    list.Add(
                        new LocalDB(Path.GetRelativePath(Global.LocalDBPath, directory), _logger)
                    );
                }
            }

            return list;
        }

        private bool DirectoryHasDBFiles(string targetDirectory)
        {
            // Process the list of files found in the directory.
            string [] fileEntries = Directory.GetFiles(targetDirectory, "??.txt");
            return fileEntries.Length >= 2;
        }
    }
}
