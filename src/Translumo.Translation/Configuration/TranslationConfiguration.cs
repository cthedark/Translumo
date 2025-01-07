using System.Collections.Generic;
using Translumo.Infrastructure.Language;
using Translumo.Utils;

namespace Translumo.Translation.Configuration
{
    public class TranslationConfiguration : BindableBase
    {
        public static TranslationConfiguration Default => new TranslationConfiguration()
        {
            TranslateFromLang = Languages.English,
            TranslateToLang = Languages.Russian,
            ProxySettings = new List<Proxy>(),
            UseLocalDB = false,
            UseLocalDBDir = "",
            AppendToLocalDB = false,
        };

        public Languages TranslateFromLang
        {
            get => _translateFromLang;
            set
            {
                SetProperty(ref _translateFromLang, value);
            }
        }

        public Languages TranslateToLang
        {
            get => _translateToLang;
            set
            {
                SetProperty(ref _translateToLang, value);
            }
        }

        public Translators Translator
        {
            get => _translator;
            set
            {
                SetProperty(ref _translator, value);
            }
        }

        public List<Proxy> ProxySettings
        {
            get => _proxySettings;
            set
            {
                SetProperty(ref _proxySettings, value);
            }
        }

        // If this is true, translator will do a local DB look up before sending a translation request.
        public bool UseLocalDB
        {
            get => _useLocalDB;
            set
            {
                SetProperty(ref _useLocalDB, value);
            }
        }

        // Directory where the local DB files are located at.
        public string UseLocalDBDir
        {
            get => _useLocalDBDir;
            set
            {
                SetProperty(ref _useLocalDBDir, value);
            }
        }

        // If this is true, newly translated sentences will be appended to the local DB, updating it.
        public bool AppendToLocalDB
        {
            get => _appendToLocalDB;
            set
            {
                SetProperty(ref _appendToLocalDB, value);
            }
        }

        private Languages _translateFromLang;
        private Languages _translateToLang;
        private Translators _translator;
        private List<Proxy> _proxySettings = new List<Proxy>();
        private bool _useLocalDB;
        private string _useLocalDBDir;
        private bool _appendToLocalDB;
    }
}
