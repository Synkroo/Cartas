using JuegoDeCartas.Enemies;
using JuegoDeCartas.Cards;
using UnityEditor;
using UnityEngine;

namespace JuegoDeCartas.EditorTools
{
    [CustomEditor(typeof(EnemyData))]
    public class EnemyDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawProperty("enemyName");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
            DrawProperty("maxHealth");
            DrawProperty("startArmor");
            DrawProperty("minDamage");
            DrawProperty("maxDamage");
            DrawProperty("goldRewardOverride");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tipo", EditorStyles.boldLabel);
            DrawProperty("enemyTier");

            EnemyTier tier = (EnemyTier)serializedObject.FindProperty("enemyTier").enumValueIndex;
            if (tier != EnemyTier.Normal)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Mecanicas", EditorStyles.boldLabel);
                DrawMechanicsList(serializedObject.FindProperty("mechanics"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
            DrawProperty("sprite");
            DrawProperty("animatorController");

            serializedObject.ApplyModifiedProperties();
        }

        void DrawProperty(string name, bool includeChildren = false)
        {
            SerializedProperty property = serializedObject.FindProperty(name);
            if (property != null)
                EditorGUILayout.PropertyField(property, includeChildren);
        }

        void DrawMechanicsList(SerializedProperty mechanicsProperty)
        {
            if (mechanicsProperty == null)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cantidad", GUILayout.Width(60f));
            int newSize = EditorGUILayout.IntField(mechanicsProperty.arraySize);
            if (newSize != mechanicsProperty.arraySize)
                mechanicsProperty.arraySize = Mathf.Max(0, newSize);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);

            for (int i = 0; i < mechanicsProperty.arraySize; i++)
            {
                SerializedProperty element = mechanicsProperty.GetArrayElementAtIndex(i);
                SerializedProperty nameProperty = element.FindPropertyRelative("mechanicName");
                SerializedProperty typeProperty = element.FindPropertyRelative("mechanicType");
                string mechanicName = string.IsNullOrWhiteSpace(nameProperty.stringValue) ? "Mecanica " + (i + 1) : nameProperty.stringValue;
                string header = mechanicName + " [" + ((EnemyMechanicType)typeProperty.enumValueIndex) + "]";

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, header, true, EditorStyles.foldoutHeader);

                if (GUILayout.Button("Eliminar", GUILayout.Width(70f)))
                {
                    mechanicsProperty.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();

                if (element.isExpanded)
                    DrawMechanicFields(element);

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Anadir mecanica"))
                mechanicsProperty.arraySize++;

            EditorGUILayout.EndVertical();
        }

        void DrawMechanicFields(SerializedProperty mechanicProperty)
        {
            SerializedProperty nameProperty = mechanicProperty.FindPropertyRelative("mechanicName");
            SerializedProperty typeProperty = mechanicProperty.FindPropertyRelative("mechanicType");

            EditorGUILayout.PropertyField(nameProperty, new GUIContent("Nombre"));
            EditorGUILayout.PropertyField(typeProperty, new GUIContent("Tipo"));

            EnemyMechanicType type = (EnemyMechanicType)typeProperty.enumValueIndex;
            switch (type)
            {
                case EnemyMechanicType.StatsPerTurn:
                    EditorGUILayout.Space(2f);
                    EditorGUILayout.LabelField("Stats Por Turno", EditorStyles.boldLabel);
                    DrawInt(mechanicProperty, "armorPerTurn", "Armadura / turno");
                    DrawInt(mechanicProperty, "healPerTurn", "Curacion / turno");
                    DrawInt(mechanicProperty, "damageRampPerTurn", "Danio extra / turno");
                    DrawInt(mechanicProperty, "firstTriggerTurn", "Primer turno");
                    break;

                case EnemyMechanicType.AddJunkToDeckOnTurnStart:
                    EditorGUILayout.Space(2f);
                    EditorGUILayout.LabelField("Cartas Basura", EditorStyles.boldLabel);
                    DrawCard(mechanicProperty, "junkCard", "Carta basura");
                    DrawInt(mechanicProperty, "cardsToAdd", "Cartas a meter");
                    DrawEnum(mechanicProperty, "cardDestination", "Destino");
                    DrawBool(mechanicProperty, "shuffleIntoDrawPile", "Barajar al meter");
                    DrawInt(mechanicProperty, "firstTriggerTurn", "Primer turno");
                    DrawInt(mechanicProperty, "triggerEveryXTurns", "Cada X turnos");
                    break;

                case EnemyMechanicType.ReviveOnDeath:
                    EditorGUILayout.Space(2f);
                    EditorGUILayout.LabelField("Revivir", EditorStyles.boldLabel);
                    DrawInt(mechanicProperty, "reviveCount", "Numero de revives");
                    DrawFloat(mechanicProperty, "maxHealthPercentOnRevive", "Vida maxima %");
                    DrawFloat(mechanicProperty, "armorPercentOnRevive", "Armadura %");
                    DrawFloat(mechanicProperty, "minDamagePercentOnRevive", "Min danio %");
                    DrawFloat(mechanicProperty, "maxDamagePercentOnRevive", "Max danio %");
                    break;
            }
        }

        static void DrawInt(SerializedProperty root, string propertyName, string label)
        {
            SerializedProperty property = root.FindPropertyRelative(propertyName);
            property.intValue = EditorGUILayout.IntField(label, property.intValue);
        }

        static void DrawBool(SerializedProperty root, string propertyName, string label)
        {
            SerializedProperty property = root.FindPropertyRelative(propertyName);
            property.boolValue = EditorGUILayout.Toggle(label, property.boolValue);
        }

        static void DrawFloat(SerializedProperty root, string propertyName, string label)
        {
            SerializedProperty property = root.FindPropertyRelative(propertyName);
            property.floatValue = EditorGUILayout.FloatField(label, property.floatValue);
        }

        static void DrawEnum(SerializedProperty root, string propertyName, string label)
        {
            SerializedProperty property = root.FindPropertyRelative(propertyName);
            property.enumValueIndex = EditorGUILayout.Popup(label, property.enumValueIndex, property.enumDisplayNames);
        }

        static void DrawCard(SerializedProperty root, string propertyName, string label)
        {
            SerializedProperty property = root.FindPropertyRelative(propertyName);
            property.objectReferenceValue = EditorGUILayout.ObjectField(
                label,
                property.objectReferenceValue,
                typeof(CardData),
                false
            );
        }
    }
}
