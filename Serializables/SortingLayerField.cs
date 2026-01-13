using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq; 
#endif

namespace Dessentials.Serializables
{
    /// <summary>
    /// A serializable class that holds a reference to a Unity Sorting Layer.
    /// It stores the layer's unique ID, which is more robust than storing the name.
    /// </summary>
    [System.Serializable]
    public class SortingLayerField
    {
        [SerializeField]
        private int m_LayerID = 0; // Default to the first layer, "Default"

        public int LayerID
        {
            get => m_LayerID;
            
            set => m_LayerID = value;
        }

        // You can add helper properties if you need the layer name or index at runtime
        public string LayerName
        {
            get => SortingLayer.IDToName(m_LayerID);
            
            set => m_LayerID = SortingLayer.NameToID(value);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// The custom PropertyDrawer for the SortingLayerField class.
    /// This script renders a dropdown menu in the Inspector with all available Sorting Layers.
    /// </summary>
    [CustomPropertyDrawer(typeof(SortingLayerField))]
    public class SortingLayerFieldDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Find the serialized property for our layer ID
            SerializedProperty layerIDProp = property.FindPropertyRelative("m_LayerID");

            // Begin drawing the property field
            EditorGUI.BeginProperty(position, label, property);

            // Get all sorting layer names
            string[] layerNames = SortingLayer.layers.Select(l => l.name).ToArray();
        
            // Find the index of the currently selected layer ID
            int currentLayerId = layerIDProp.intValue;
            int currentLayerIndex = -1;
            for (int i = 0; i < SortingLayer.layers.Length; i++)
            {
                if (SortingLayer.layers[i].id == currentLayerId)
                {
                    currentLayerIndex = i;
                    break;
                }
            }
        
            // If the saved ID is invalid (e.g., layer was deleted), default to the first layer
            if (currentLayerIndex == -1)
            {
                currentLayerIndex = 0;
                layerIDProp.intValue = SortingLayer.layers[0].id;
            }

            // Create the dropdown popup
            int newLayerIndex = EditorGUI.Popup(position, label.text, currentLayerIndex, layerNames);

            // If the user selected a new layer, update the ID
            if (newLayerIndex != currentLayerIndex)
            {
                layerIDProp.intValue = SortingLayer.layers[newLayerIndex].id;
            }

            // End drawing the property field
            EditorGUI.EndProperty();
        }
    }
#endif
}
