using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Translumo.Infrastructure.Constants;

namespace Translumo.Translation
{
    // Responsible for writing to Local DBs
    public class LocalDB
    {
        private readonly ILogger _logger;

        private readonly string _directory;

        private string _fromLanguageCode;
        private string _targetLanguageCode;

        private IDictionary<string, string> _dbMap;
        private IList<Tuple<string, string>> _newTranslations;

        public string Directory {
            get => _directory;
        }

        public LocalDB(string directory, ILogger logger)
        {
            _logger = logger;
            _directory = directory;
        }

        public bool SetLanguagePair(string from, string target) {
            FlushUpdatedTranslations();
            _fromLanguageCode = from;
            _targetLanguageCode = target;
            return InitializeMap();
        }

        // Attempt to get the translation from the local db map. Returns null if no match.
        public string GetTranslation(string from) {
            if (!IsReady()) {
                _logger.LogWarning($"LocalDB tried to get translation while not ready: '{from}'");
                return null;
            }

            string value;
            bool matched = _dbMap.TryGetValue(from, out value);
            if (matched) {
                return value;
            } else {
                _logger.LogTrace($"LocalDB could not find a matching translation for '{from}'");
                return null;
            }
        }

        // This adds a new translation to the existing map.
        public void UpdateTranslation(string from, string to) {
            if (!IsReady()) {
                _logger.LogWarning($"LocalDB tried to update translation while not ready: '{from}' -> '{to}'");
                return;
            }
            _logger.LogTrace($"writing the newly translated line '{from}' -> '{to}' to local db");

            _dbMap.Add(from, to);
            _newTranslations.Add(new Tuple<string, string>(from, to));
        }

        public void FlushUpdatedTranslations() {
            if (_newTranslations != null && _newTranslations.Count > 0) {
                _logger.LogTrace($"LocalDB writing {_newTranslations.Count} translations to files");
                var paths = GetPaths(); // (fromPath, toPath)
                string fromPath = paths.Item1;
                string toPath = paths.Item2;
                long count = 0;

                try{
                    StreamWriter fromWriter = new StreamWriter(fromPath);
                    StreamWriter toWriter = new StreamWriter(toPath);

                    foreach (Tuple<string, string> translation in _newTranslations)
                    {
                        fromWriter.WriteLine(translation.Item1, true, Encoding.Unicode);
                        toWriter.WriteLine(translation.Item2, true, Encoding.Unicode);
                        count ++;
                    }

                    fromWriter.Flush();
                    toWriter.Flush();
                    fromWriter.Close();
                    toWriter.Close();
                }
                catch (Exception e)
                {
                    _logger.LogError($"LocalDB encountered an error while writing new translations to files '{e.Message}'");
                }
                finally
                {
                    _logger.LogTrace($"done writing '{count} new translations to files'");
                }

                _newTranslations = new List<Tuple<string, string>>();
            }
        }

        private bool InitializeMap() {
            if (IsLanguagePairSet()) {
                _dbMap = new Dictionary<string, string>();
                _newTranslations = new List<Tuple<string, string>>();
                var paths = GetPaths(); // (fromPath, toPath)
                string fromPath = paths.Item1;
                string toPath = paths.Item2;
                long count = 0;
                try
                {
                    StreamReader srFrom = new StreamReader(fromPath);
                    StreamReader srTo = new StreamReader(toPath);
                    string fromLine = srFrom.ReadLine();
                    string toLine = srTo.ReadLine();
                    while (fromLine != null)
                    {
                        _dbMap.Add(fromLine, toLine);
                        fromLine = srFrom.ReadLine();
                        toLine = srTo.ReadLine();
                        count += 1;
                    }
                    srFrom.Close();
                    srTo.Close();
                }
                catch(Exception e)
                {
                    _logger.LogError($"LocalDB encounter an exception while reading from files: {e.Message}");
                    return false;
                }
                finally
                {
                    _logger.LogTrace($"LocalDB successfully read {count} lines of translation data");
                }
            } else {
                _logger.LogWarning("LocalDB tried to initialize map without languages set.");
                return false;
            }

            return true;
        }

        private bool IsLanguagePairSet() {
            return !string.IsNullOrWhiteSpace(_fromLanguageCode) && !string.IsNullOrWhiteSpace(_targetLanguageCode);
        }

        private bool IsReady() {
            return IsLanguagePairSet() && _dbMap != null;
        }

        // Returns a tuple of file paths <from, target>
        private Tuple<string, string> GetPaths() {
            string subdir = "";
            if (!_directory.Equals(LocalDBScanner.ROOT_DIRECTORY_DISPLAY)) {
                subdir = $"{_directory}\\";
            }
            string fromPath = $"{Global.LocalDBPath}\\{subdir}{_fromLanguageCode}.txt";
            string toPath = $"{Global.LocalDBPath}\\{subdir}{_targetLanguageCode}.txt";
            return new Tuple<string, string>(fromPath, toPath);
        }
    }
}
