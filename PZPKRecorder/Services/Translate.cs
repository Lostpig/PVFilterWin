﻿
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace PZPKRecorder.Services;

internal static class Translate
{
    const string DefaultLanguage = "zh-CN";
    public static string Current { get; private set; } = DefaultLanguage;
    private static readonly List<string> _languages = new();
    public static IList<string> Languages => _languages;
    public static event EventHandler<string>? LanguageChanged;

    public static void Init()
    {
        string rootPath = System.AppDomain.CurrentDomain.BaseDirectory;
        string dirPath = Path.Join(rootPath, "Localization");

        string[] files = Directory.GetFiles(dirPath);
        string[] langFiles = files
            .Where(f => Path.GetExtension(f).ToLower() == ".json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .ToArray();

        _languages.Clear();
        _languages.AddRange(langFiles);

        string userSetLang = ReadLanguageSet();
        LoadLanguage(userSetLang);
        Current = userSetLang;
    }

    public static void ChangeLanguage(string language)
    {
        Current = language;
        SaveLanguageSet(language);

        LoadLanguage(language);

        LanguageChanged?.Invoke(null, language);
    }

    private static string ReadLanguageSet()
    {
        string? userSetCurrent = VariantService.GetVariant("language");
        if (String.IsNullOrEmpty(userSetCurrent))
        {
            userSetCurrent = DefaultLanguage;
            VariantService.SetVariant("language", userSetCurrent);
        }
        return userSetCurrent;
    }
    private static void SaveLanguageSet(string lang)
    {
        VariantService.SetVariant("language", lang);
    }

    private static void LoadLanguage(string lang)
    {
        string rootPath = System.AppDomain.CurrentDomain.BaseDirectory;
        string langPath = Path.Join(rootPath, "Localization", $"{lang}.json");
        Dictionary<string, string>? map;
        if (File.Exists(langPath))
        {
            string langJson = File.ReadAllText(langPath, Encoding.UTF8);
            map = JsonConvert.DeserializeObject<Dictionary<string, string>>(langJson);
        }
        else
        {
            map = null;
            Debug.Assert(false, $"language file {lang}.json not found");
        }

        UpdateLanguage(map);
    }
    private static void UpdateLanguage(Dictionary<string, string>? languageMap)
    {
        Type t = typeof(Localization.LocalizeDict);
        foreach (PropertyInfo pi in t.GetProperties())
        {
            Attribute? attr = pi.GetCustomAttribute(typeof(TranslateBindAttribute));
            if (attr is TranslateBindAttribute trans)
            {
                pi.SetValue(null, getText(trans.Key));
            }
        }

        string getText(string key)
        {
            if (languageMap != null && languageMap.ContainsKey(key))
            {
                return languageMap[key];
            }
            return key;
        }
    }
}

[AttributeUsage(AttributeTargets.Property)]
internal class TranslateBindAttribute : Attribute
{
    public string Key { get; private set; }
    public TranslateBindAttribute(string key)
    {
        Key = key;
    }
}
