﻿using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Helpers;
using D4Companion.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace D4Companion.Services
{
    public class AffixManager : IAffixManager
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;
        private readonly ISettingsManager _settingsManager;

        private List<AffixInfo> _affixes = new List<AffixInfo>();
        private List<AffixInfo> _affixesEnUS = new List<AffixInfo>();
        private List<AffixInfo> _affixesEnUSFull = new List<AffixInfo>();
        private List<AffixPreset> _affixPresets = new List<AffixPreset>();
        private List<AspectInfo> _aspects = new List<AspectInfo>();
        private List<SigilInfo> _sigils = new List<SigilInfo>();
        private Dictionary<string, string> _sigilDungeonTiers = new Dictionary<string, string>(); // <sigilId, tier>

        // Start of Constructors region

        #region Constructors

        public AffixManager(IEventAggregator eventAggregator, ILogger<AffixManager> logger, ISettingsManager settingsManager)
        {
            // Init IEventAggregator
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<AffixLanguageChangedEvent>().Subscribe(HandleAffixLanguageChangedEvent);

            // Init services
            _settingsManager = settingsManager;

            // Init logger
            _logger = logger;

            // Init store data
            InitAffixData();
            InitAffixDataEnUS();
            InitAspectData();
            InitSigilData();
            InitSigilDungeonTierData();

            // Load affix presets
            LoadAffixPresets();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<AffixInfo> Affixes { get => _affixes; set => _affixes = value; }
        public List<AffixPreset> AffixPresets { get => _affixPresets; }
        public List<AspectInfo> Aspects { get => _aspects; set => _aspects = value; }
        public List<SigilInfo> Sigils { get => _sigils; set => _sigils = value; }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void HandleAffixLanguageChangedEvent()
        {
            InitAffixData();
            InitAspectData();
            InitSigilData();
        }

        #endregion

        // Start of Methods region

        #region Methods

        public void AddAffixPreset(AffixPreset affixPreset)
        {
            _affixPresets.Add(affixPreset);

            // Sort list
            _affixPresets.Sort((x, y) =>
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            });

            SaveAffixPresets();

            _eventAggregator.GetEvent<AffixPresetAddedEvent>().Publish();
        }

        public void RemoveAffixPreset(AffixPreset affixPreset)
        {
            if (affixPreset == null) return;

            _affixPresets.Remove(affixPreset);

            // Sort list
            _affixPresets.Sort((x, y) =>
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            });

            SaveAffixPresets();

            _eventAggregator.GetEvent<AffixPresetRemovedEvent>().Publish();
        }

        public void AddAffix(AffixInfo affixInfo, string itemType)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            if (!preset.ItemAffixes.Any(a => a.Id.Equals(affixInfo.IdName) && a.Type.Equals(itemType)))
            {
                preset.ItemAffixes.Add(new ItemAffix
                {
                    Id = affixInfo.IdName,
                    Type = itemType
                });
                SaveAffixPresets();
            }

            _eventAggregator.GetEvent<SelectedAffixesChangedEvent>().Publish();
        }

        public void RemoveAffix(AffixInfo affixInfo, string itemType)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            if (preset.ItemAffixes.RemoveAll(a => a.Id.Equals(affixInfo.IdName) && a.Type.Equals(itemType)) > 0)
            {
                SaveAffixPresets();
            }

            _eventAggregator.GetEvent<SelectedAffixesChangedEvent>().Publish();
        }

        public void RemoveAffix(ItemAffix itemAffix)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            if (preset.ItemAffixes.RemoveAll(a => a.Id.Equals(itemAffix.Id) && a.Type.Equals(itemAffix.Type)) > 0)
            {
                SaveAffixPresets();
            }

            _eventAggregator.GetEvent<SelectedAffixesChangedEvent>().Publish();
        }

        public void AddAspect(AspectInfo aspectInfo, string itemType)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            if (!preset.ItemAspects.Any(a => a.Id.Equals(aspectInfo.IdName) && a.Type.Equals(itemType)))
            {
                preset.ItemAspects.Add(new ItemAffix
                {
                    Id = aspectInfo.IdName,
                    Type = itemType
                });
                SaveAffixPresets();
            }

            _eventAggregator.GetEvent<SelectedAspectsChangedEvent>().Publish();
        }

        public void RemoveAspect(ItemAffix itemAffix)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            if (preset.ItemAspects.RemoveAll(a => a.Id.Equals(itemAffix.Id)) > 0)
            {
                SaveAffixPresets();
            }

            _eventAggregator.GetEvent<SelectedAspectsChangedEvent>().Publish();
        }

        public void AddSigil(SigilInfo sigilInfo, string itemType)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            if (!preset.ItemSigils.Any(a => a.Id.Equals(sigilInfo.IdName) && a.Type.Equals(itemType)))
            {
                preset.ItemSigils.Add(new ItemAffix
                {
                    Id = sigilInfo.IdName,
                    Type = itemType,
                    Color = _settingsManager.Settings.SelectedSigilDisplayMode.Equals("Whitelisting") ? Colors.Green : Colors.Red
                });
                SaveAffixPresets();
            }

            _eventAggregator.GetEvent<SelectedSigilsChangedEvent>().Publish();
        }

        public void RemoveSigil(ItemAffix itemAffix)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return;

            if (preset.ItemSigils.RemoveAll(a => a.Id.Equals(itemAffix.Id)) > 0)
            {
                SaveAffixPresets();
            }

            _eventAggregator.GetEvent<SelectedSigilsChangedEvent>().Publish();
        }

        private void InitAffixData()
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;

            _affixes.Clear();
            string resourcePath = @$".\Data\Affixes.{language}.json";
            using (FileStream? stream = File.OpenRead(resourcePath))
            {
                if (stream != null)
                {
                    // create the options
                    var options = new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    };
                    // register the converter
                    options.Converters.Add(new BoolConverter());
                    options.Converters.Add(new IntConverter());

                    _affixes = JsonSerializer.Deserialize<List<AffixInfo>>(stream, options) ?? new List<AffixInfo>();
                }
            }
        }

        /// <summary>
        /// Used for Maxroll builds to import builds when app setting is not set to English.
        /// </summary>
        private void InitAffixDataEnUS()
        {
            _affixesEnUS.Clear();
            string resourcePath = @$".\Data\Affixes.enUS.json";
            using (FileStream? stream = File.OpenRead(resourcePath))
            {
                if (stream != null)
                {
                    // create the options
                    var options = new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    };
                    // register the converter
                    options.Converters.Add(new BoolConverter());
                    options.Converters.Add(new IntConverter());

                    _affixesEnUS = JsonSerializer.Deserialize<List<AffixInfo>>(stream, options) ?? new List<AffixInfo>();
                }
            }

            _affixesEnUSFull.Clear();
            string resourcePathFull = @$".\Data\Affixes.Full.enUS.json";
            using (FileStream? stream = File.OpenRead(resourcePathFull))
            {
                if (stream != null)
                {
                    // create the options
                    var options = new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    };
                    // register the converter
                    options.Converters.Add(new BoolConverter());
                    options.Converters.Add(new IntConverter());

                    _affixesEnUSFull = JsonSerializer.Deserialize<List<AffixInfo>>(stream, options) ?? new List<AffixInfo>();
                }
            }
        }

        private void InitAspectData()
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;

            _aspects.Clear();
            string resourcePath = @$".\Data\Aspects.{language}.json";
            using (FileStream? stream = File.OpenRead(resourcePath))
            {
                if (stream != null)
                {
                    // create the options
                    var options = new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    };
                    // register the converter
                    options.Converters.Add(new BoolConverter());
                    options.Converters.Add(new IntConverter());

                    _aspects = JsonSerializer.Deserialize<List<AspectInfo>>(stream, options) ?? new List<AspectInfo>();
                }
            }
        }

        private void InitSigilData()
        {
            string language = _settingsManager.Settings.SelectedAffixLanguage;

            _sigils.Clear();
            string resourcePath = @$".\Data\Sigils.{language}.json";
            using (FileStream? stream = File.OpenRead(resourcePath))
            {
                if (stream != null)
                {
                    // create the options
                    var options = new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    };
                    // register the converter
                    options.Converters.Add(new BoolConverter());
                    options.Converters.Add(new IntConverter());

                    _sigils = JsonSerializer.Deserialize<List<SigilInfo>>(stream, options) ?? new List<SigilInfo>();
                }
            }
        }

        private void InitSigilDungeonTierData()
        {
            try
            {
                _sigilDungeonTiers.Clear();
                string resourcePath = @$".\Config\DungeonTiers.json";
                using (FileStream? stream = File.OpenRead(resourcePath))
                {
                    if (stream != null)
                    {
                        // create the options
                        var options = new JsonSerializerOptions()
                        {
                            WriteIndented = true
                        };
                        // register the converter
                        options.Converters.Add(new BoolConverter());
                        options.Converters.Add(new IntConverter());

                        _sigilDungeonTiers = JsonSerializer.Deserialize<Dictionary<string, string>>(stream, options) ?? new Dictionary<string, string> { };
                    }
                }
            }
            catch { }
        }

        public bool IsAffixSelected(AffixInfo affixInfo, string itemType)
        {
            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return false;

            var affix = preset.ItemAffixes.FirstOrDefault(a => a.Id.Equals(affixInfo.IdName) && a.Type.Equals(itemType));
            if (affix == null) return false;

            return true;
        }

        public ItemAffix GetAffix(string affixId, string itemType)
        {
            var affixDefault = new ItemAffix
            {
                Id = affixId,
                Type = itemType,
                Color = Colors.Red
            };

            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return affixDefault;

            // Ignore item type for Temper Manual.
            var affix = itemType.Equals(Constants.ItemTypeConstants.Temper) ?
                preset.ItemAffixes.FirstOrDefault(a => a.Id.Equals(affixId)) :
                preset.ItemAffixes.FirstOrDefault(a => a.Id.Equals(affixId) && a.Type.Equals(itemType));
            if (affix == null) return affixDefault;
            return affix;
        }

        public string GetAffixDescription(string affixId)
        {
            var affixInfo = _affixes.FirstOrDefault(a => a.IdName.Equals(affixId));
            if (affixInfo != null)
            {
                return affixInfo.Description;
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetAffixId(int affixSno)
        {
            var affixInfo = _affixes.FirstOrDefault(a => a.IdSno == affixSno);
            if (affixInfo != null)
            {
                return affixInfo.IdName;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Find matching Affix using it's description for affixes used by imported Maxroll builds.
        /// </summary>
        /// <param name="affixInfo"></param>
        /// <returns></returns>
        public AffixInfo? GetAffixInfoEnUS(AffixInfo affixInfo)
        {
            return _affixesEnUS.FirstOrDefault(a => a.DescriptionClean.Equals(affixInfo.DescriptionClean));
        }

        /// <summary>
        /// Find Affix with matching sno for affixes used by imported Maxroll builds.
        /// Uses an affix list contaning all known affixes, included affixes with duplicated descriptions.
        /// </summary>
        /// <param name="affixSno"></param>
        /// <returns></returns>
        public AffixInfo? GetAffixInfoEnUSFull(int affixSno)
        {
            return _affixesEnUSFull.FirstOrDefault(a => a.IdSno == affixSno);
        }

        public ItemAffix GetAspect(string aspectId, string itemType)
        {
            var affixDefault = new ItemAffix
            {
                Id = aspectId,
                Type = itemType,
                Color = Colors.Red
            };

            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return affixDefault;

            var aspect = preset.ItemAspects.FirstOrDefault(a => a.Id.Equals(aspectId) && a.Type.Equals(itemType));
            if (aspect == null) return affixDefault;
            return aspect;
        }

        public string GetAspectDescription(string aspectId)
        {
            var aspectInfo = _aspects.FirstOrDefault(a => a.IdName.Equals(aspectId));
            if (aspectInfo != null)
            {
                return aspectInfo.Description;
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetAspectId(int aspectSno)
        {
            var aspectInfo = _aspects.FirstOrDefault(a => a.IdSno == aspectSno);
            if (aspectInfo != null)
            {
                return aspectInfo.IdName;
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetAspectName(string aspectId)
        {
            var aspectInfo = _aspects.FirstOrDefault(a => a.IdName.Equals(aspectId));
            if (aspectInfo != null)
            {
                return aspectInfo.Name;
            }
            else
            {
                return string.Empty;
            }
        }

        public ItemAffix GetSigil(string affixId, string itemType)
        {
            var affixDefault = new ItemAffix
            {
                Id = affixId,
                Type = itemType,
                Color = _settingsManager.Settings.SelectedSigilDisplayMode.Equals("Whitelisting") ? Colors.Red : Colors.Green
            };

            var preset = _affixPresets.FirstOrDefault(preset => preset.Name.Equals(_settingsManager.Settings.SelectedAffixPreset));
            if (preset == null) return affixDefault;

            var affix = preset.ItemSigils.FirstOrDefault(a => a.Id.Equals(affixId) && a.Type.Equals(itemType));
            if (affix == null) return affixDefault;
            return affix;
        }

        public string GetSigilDescription(string sigilId)
        {
            var sigilInfo = _sigils.FirstOrDefault(a => a.IdName.Equals(sigilId));
            if (sigilInfo != null)
            {
                return sigilInfo.Description;
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetSigilDungeonTier(string sigilId)
        {
            return _sigilDungeonTiers.TryGetValue(sigilId, out var tier) ? tier : "F";
        }

        public string GetSigilType(string sigilId)
        {
            var sigilInfo = _sigils.FirstOrDefault(a => a.IdName.Equals(sigilId));
            if (sigilInfo != null)
            {
                return sigilInfo.Type;
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetSigilName(string sigilId)
        {
            var sigilInfo = _sigils.FirstOrDefault(a => a.IdName.Equals(sigilId));
            if (sigilInfo != null)
            {
                return sigilInfo.Name;
            }
            else
            {
                return string.Empty;
            }
        }

        public void SetSigilDungeonTier(SigilInfo sigilInfo, string tier)
        {
            _sigilDungeonTiers[sigilInfo.IdName] = tier;
            _eventAggregator.GetEvent<SelectedSigilDungeonTierChangedEvent>().Publish();

            SaveSigilDungeonTierData();
        }

        public string GetGearOrSigilAffixDescription(string affixId)
        {
            var affixInfo = _affixes.FirstOrDefault(a => a.IdName.Equals(affixId));
            var sigilInfo = _sigils.FirstOrDefault(a => a.IdName.Equals(affixId));
            if (affixInfo != null) return affixInfo.Description;
            if (sigilInfo != null) return sigilInfo.Name;

            return string.Empty;
        }

        public void SaveAffixColor(ItemAffix itemAffix)
        {
            SaveAffixPresets();

            _eventAggregator.GetEvent<SelectedAffixesChangedEvent>().Publish();
            _eventAggregator.GetEvent<SelectedAspectsChangedEvent>().Publish();
            _eventAggregator.GetEvent<SelectedSigilsChangedEvent>().Publish();
        }

        private void LoadAffixPresets()
        {
            _affixPresets.Clear();

            string fileName = "Config/AffixPresets-v2.json";
            if (File.Exists(fileName))
            {
                using FileStream stream = File.OpenRead(fileName);
                _affixPresets = JsonSerializer.Deserialize<List<AffixPreset>>(stream) ?? new List<AffixPreset>();
            }

            // Sort list
            _affixPresets.Sort((x, y) =>
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            });

            SaveAffixPresets();
        }

        private void SaveAffixPresets()
        {
            string fileName = "Config/AffixPresets-v2.json";
            string path = Path.GetDirectoryName(fileName) ?? string.Empty;
            Directory.CreateDirectory(path);

            using FileStream stream = File.Create(fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonSerializer.Serialize(stream, AffixPresets, options);
        }

        private void SaveSigilDungeonTierData()
        {
            string fileName = "./Config/DungeonTiers.json";
            string path = Path.GetDirectoryName(fileName) ?? string.Empty;
            Directory.CreateDirectory(path);

            using FileStream stream = File.Create(fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonSerializer.Serialize(stream, _sigilDungeonTiers, options);
        }

        #endregion
    }
}
