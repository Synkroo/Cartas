using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JuegoDeCartas.Progression;

namespace JuegoDeCartas.UI
{
    public class ProfileSlotUI : MonoBehaviour
    {
        public int slotIndex;
        public TMP_InputField nameInput;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI completionText;
        public Button loadButton;
        public Button saveButton;
        public Button deleteButton;
        public Button renameButton;

        Action<int> onLoad;
        Action<int> onSave;
        Action<int> onDelete;
        Action<int, string> onRename;

        void Awake()
        {
            if (loadButton != null)
                loadButton.onClick.AddListener(() => onLoad?.Invoke(slotIndex));
            if (saveButton != null)
                saveButton.onClick.AddListener(() => onSave?.Invoke(slotIndex));
            if (deleteButton != null)
                deleteButton.onClick.AddListener(() => onDelete?.Invoke(slotIndex));
            if (renameButton != null)
                renameButton.onClick.AddListener(() => onRename?.Invoke(slotIndex, nameInput != null ? nameInput.text : ""));
        }

        public void Setup(
            ProfileInfo info,
            float completion,
            Action<int> load,
            Action<int> save,
            Action<int> delete,
            Action<int, string> rename)
        {
            onLoad = load;
            onSave = save;
            onDelete = delete;
            onRename = rename;

            if (nameInput != null)
                nameInput.SetTextWithoutNotify(info.Name);
            if (statusText != null)
            {
                string saved = string.IsNullOrWhiteSpace(info.LastSaved) ? "Sin guardado manual" : info.LastSaved;
                statusText.text = info.IsActive ? "Activo - " + saved : info.Exists ? saved : "Vacio";
            }
            if (completionText != null)
                completionText.text = Mathf.RoundToInt(completion * 100f) + "%";
            if (saveButton != null)
                saveButton.interactable = info.IsActive;
            if (deleteButton != null)
                deleteButton.interactable = info.Exists;
        }
    }
}
