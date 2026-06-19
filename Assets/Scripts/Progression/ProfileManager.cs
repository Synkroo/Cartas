using System;
using UnityEngine;

namespace JuegoDeCartas.Progression
{
    public readonly struct ProfileInfo
    {
        public readonly int Slot;
        public readonly string Name;
        public readonly bool Exists;
        public readonly string LastSaved;
        public readonly bool IsActive;

        public ProfileInfo(int slot, string name, bool exists, string lastSaved, bool isActive)
        {
            Slot = slot;
            Name = name;
            Exists = exists;
            LastSaved = lastSaved;
            IsActive = isActive;
        }
    }

    public static class ProfileManager
    {
        public const int SlotCount = 3;
        const int TemporarySlot = -1;
        const string ActiveSlotKey = "Profiles_ActiveSlot";
        const string TemporaryKey = "Profiles_Temporary";

        public static event Action ProfileChanged;

        public static int ActiveSlot
        {
            get
            {
                EnsureInitialized();
                return PlayerPrefs.GetInt(ActiveSlotKey, 0);
            }
        }

        public static bool IsTemporary
        {
            get
            {
                EnsureInitialized();
                return PlayerPrefs.GetInt(TemporaryKey, 0) == 1;
            }
        }

        public static string ActiveProfileName =>
            IsTemporary ? "Perfil temporal (100%)" : GetInfo(ActiveSlot).Name;

        public static ProfileInfo GetInfo(int slot)
        {
            ValidateSlot(slot);
            bool exists = PlayerPrefs.GetInt(MetaKey(slot, "Exists"), slot == 0 ? 1 : 0) == 1;
            string name = PlayerPrefs.GetString(MetaKey(slot, "Name"), $"Perfil {slot + 1}");
            string lastSaved = PlayerPrefs.GetString(MetaKey(slot, "LastSaved"), "");
            return new ProfileInfo(slot, name, exists, lastSaved, !IsTemporary && ActiveSlot == slot);
        }

        public static void Load(int slot)
        {
            ValidateSlot(slot);
            EnsureProfileExists(slot);
            PlayerPrefs.SetInt(ActiveSlotKey, slot);
            PlayerPrefs.SetInt(TemporaryKey, 0);
            PlayerPrefs.Save();
            ProfileChanged?.Invoke();
        }

        public static void LoadTemporary()
        {
            EnsureInitialized();
            PlayerPrefs.SetInt(TemporaryKey, 1);
            PlayerPrefs.Save();
            ProfileChanged?.Invoke();
        }

        public static void SaveActive()
        {
            EnsureInitialized();
            if (!IsTemporary)
                PlayerPrefs.SetString(MetaKey(ActiveSlot, "LastSaved"), DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            PlayerPrefs.Save();
            ProfileChanged?.Invoke();
        }

        public static void Rename(int slot, string newName)
        {
            ValidateSlot(slot);
            EnsureProfileExists(slot);
            string value = string.IsNullOrWhiteSpace(newName)
                ? $"Perfil {slot + 1}"
                : newName.Trim();
            PlayerPrefs.SetString(MetaKey(slot, "Name"), value);
            PlayerPrefs.Save();
            ProfileChanged?.Invoke();
        }

        public static void Delete(int slot)
        {
            ValidateSlot(slot);
            int generation = PlayerPrefs.GetInt(MetaKey(slot, "Generation"), 0);
            PlayerPrefs.SetInt(MetaKey(slot, "Generation"), generation + 1);
            PlayerPrefs.SetInt(MetaKey(slot, "Exists"), 0);
            PlayerPrefs.DeleteKey(MetaKey(slot, "LastSaved"));
            PlayerPrefs.SetString(MetaKey(slot, "Name"), $"Perfil {slot + 1}");

            if (!IsTemporary && ActiveSlot == slot)
            {
                int replacement = FindExistingSlot(slot);
                EnsureProfileExists(replacement);
                PlayerPrefs.SetInt(ActiveSlotKey, replacement);
            }

            PlayerPrefs.Save();
            ProfileChanged?.Invoke();
        }

        public static string ScopedKey(string key)
        {
            EnsureInitialized();
            if (IsTemporary)
                return "Profile_Temporary_" + key;

            int slot = ActiveSlot;
            int generation = PlayerPrefs.GetInt(MetaKey(slot, "Generation"), 0);
            return $"Profile_{slot}_{generation}_{key}";
        }

        public static string ScopedKeyForSlot(int slot, string key)
        {
            ValidateSlot(slot);
            int generation = PlayerPrefs.GetInt(MetaKey(slot, "Generation"), 0);
            return $"Profile_{slot}_{generation}_{key}";
        }

        public static bool CanUseLegacyFallback =>
            !IsTemporary &&
            ActiveSlot == 0 &&
            PlayerPrefs.GetInt(MetaKey(0, "Generation"), 0) == 0;

        static void EnsureInitialized()
        {
            if (!PlayerPrefs.HasKey(ActiveSlotKey))
                PlayerPrefs.SetInt(ActiveSlotKey, 0);
            if (!PlayerPrefs.HasKey(TemporaryKey))
                PlayerPrefs.SetInt(TemporaryKey, 0);
            EnsureProfileExists(Mathf.Clamp(PlayerPrefs.GetInt(ActiveSlotKey, 0), 0, SlotCount - 1));
        }

        static void EnsureProfileExists(int slot)
        {
            if (PlayerPrefs.GetInt(MetaKey(slot, "Exists"), 0) == 1)
                return;

            PlayerPrefs.SetInt(MetaKey(slot, "Exists"), 1);
            if (!PlayerPrefs.HasKey(MetaKey(slot, "Name")))
                PlayerPrefs.SetString(MetaKey(slot, "Name"), $"Perfil {slot + 1}");
        }

        static int FindExistingSlot(int excluded)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (i != excluded && PlayerPrefs.GetInt(MetaKey(i, "Exists"), 0) == 1)
                    return i;
            }
            return excluded == 0 ? 1 : 0;
        }

        static string MetaKey(int slot, string suffix) => $"Profiles_Slot_{slot}_{suffix}";

        static void ValidateSlot(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
                throw new ArgumentOutOfRangeException(nameof(slot));
        }
    }

    public static class ProfilePrefs
    {
        public static int GetInt(string key, int defaultValue = 0)
        {
            string scoped = ProfileManager.ScopedKey(key);
            if (PlayerPrefs.HasKey(scoped))
                return PlayerPrefs.GetInt(scoped, defaultValue);
            if (ProfileManager.CanUseLegacyFallback && PlayerPrefs.HasKey(key))
                return PlayerPrefs.GetInt(key, defaultValue);
            return defaultValue;
        }

        public static void SetInt(string key, int value)
        {
            if (!ProfileManager.IsTemporary)
                PlayerPrefs.SetInt(ProfileManager.ScopedKey(key), value);
        }

        public static bool HasKey(string key)
        {
            string scoped = ProfileManager.ScopedKey(key);
            return PlayerPrefs.HasKey(scoped) ||
                   (ProfileManager.CanUseLegacyFallback && PlayerPrefs.HasKey(key));
        }

        public static void DeleteKey(string key)
        {
            if (!ProfileManager.IsTemporary)
                PlayerPrefs.DeleteKey(ProfileManager.ScopedKey(key));
        }

        public static void Save()
        {
            if (!ProfileManager.IsTemporary)
                PlayerPrefs.Save();
        }

        public static int GetIntForSlot(int slot, string key, int defaultValue = 0)
        {
            string scoped = ProfileManager.ScopedKeyForSlot(slot, key);
            if (PlayerPrefs.HasKey(scoped))
                return PlayerPrefs.GetInt(scoped, defaultValue);
            if (slot == 0 &&
                PlayerPrefs.GetInt($"Profiles_Slot_{slot}_Generation", 0) == 0 &&
                PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetInt(key, defaultValue);
            }
            return defaultValue;
        }
    }
}
