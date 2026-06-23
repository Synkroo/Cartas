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
        public TextMeshProUGUI renameButtonText;

        [Header("Text")]
        public string renameLabel = "Renombrar";
        public string saveNameLabel = "Guardar nombre";

        Action<int> onLoad;
        Action<int> onDelete;
        Action<int, string> onRename;
        bool isRenaming;

        void Awake()
        {
            if (loadButton != null)
                loadButton.onClick.AddListener(() => onLoad?.Invoke(slotIndex));
            if (deleteButton != null)
                deleteButton.onClick.AddListener(() => onDelete?.Invoke(slotIndex));
            if (renameButton != null)
                renameButton.onClick.AddListener(ToggleRename);

            if (renameButtonText == null && renameButton != null)
                renameButtonText = renameButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        public void Setup(
            ProfileInfo info,
            float completion,
            Action<int> load,
            Action<int> delete,
            Action<int, string> rename)
        {
            onLoad = load;
            onDelete = delete;
            onRename = rename;

            SetRenameMode(false);
            if (nameInput != null)
                nameInput.SetTextWithoutNotify(info.Name);
            if (statusText != null)
            {
                statusText.text = info.IsActive
                    ? "Activo - guardado automatico"
                    : info.Exists
                        ? "Guardado automatico"
                        : "Vacio";
            }
            if (completionText != null)
                completionText.text = Mathf.RoundToInt(completion * 100f) + "%";
            if (saveButton != null)
                saveButton.gameObject.SetActive(false);
            if (deleteButton != null)
                deleteButton.interactable = info.Exists;
        }

        void ToggleRename()
        {
            if (!isRenaming)
            {
                SetRenameMode(true);
                return;
            }

            string value = nameInput != null ? nameInput.text : "";
            SetRenameMode(false);
            onRename?.Invoke(slotIndex, value);
        }

        void SetRenameMode(bool editing)
        {
            isRenaming = editing;

            if (nameInput != null)
            {
                nameInput.interactable = editing;
                nameInput.readOnly = !editing;
                if (editing)
                {
                    nameInput.Select();
                    nameInput.ActivateInputField();
                }
                else
                {
                    nameInput.DeactivateInputField();
                }
            }

            if (renameButtonText != null)
                renameButtonText.text = editing ? saveNameLabel : renameLabel;
        }
    }
}
