using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

[CustomEditor(typeof(ControllerKeyboardBinder))]
public sealed class ControllerKeyboardBinderEditor : Editor
{
    private static IDisposable listenSubscription;
    private static ControllerKeyboardBinder listenTarget;
    private static int listenBindingIndex = -1;

    private SerializedProperty bindingsProperty;
    private SerializedProperty logPressedControlsProperty;
    private SerializedProperty logOnlyOncePerControlProperty;
    private SerializedProperty pressPointProperty;

    private void OnEnable()
    {
        bindingsProperty = serializedObject.FindProperty("bindings");
        logPressedControlsProperty = serializedObject.FindProperty("logPressedControls");
        logOnlyOncePerControlProperty = serializedObject.FindProperty("logOnlyOncePerControl");
        pressPointProperty = serializedObject.FindProperty("pressPoint");
    }

    private void OnDisable()
    {
        if (listenTarget == target)
        {
            StopListening();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Add a binding, choose the Keyboard Key, click Bind By Pressing Controller Button, then press the controller button.",
            MessageType.Info);

        DrawBindings();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(logPressedControlsProperty);

        using (new EditorGUI.DisabledScope(!logPressedControlsProperty.boolValue))
        {
            EditorGUILayout.PropertyField(logOnlyOncePerControlProperty);
        }

        EditorGUILayout.PropertyField(pressPointProperty);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBindings()
    {
        EditorGUILayout.LabelField("Bindings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        int size = Mathf.Max(0, EditorGUILayout.IntField("Size", bindingsProperty.arraySize));
        if (EditorGUI.EndChangeCheck())
        {
            bindingsProperty.arraySize = size;
        }

        for (int i = 0; i < bindingsProperty.arraySize; i++)
        {
            SerializedProperty binding = bindingsProperty.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Binding {i}", EditorStyles.boldLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                if (listenBindingIndex == i && listenTarget == target)
                {
                    StopListening();
                }

                bindingsProperty.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(binding.FindPropertyRelative("label"));
            EditorGUILayout.PropertyField(binding.FindPropertyRelative("keyboardKey"));

            SerializedProperty controlPath = binding.FindPropertyRelative("controlPath");
            SerializedProperty deviceContains = binding.FindPropertyRelative("deviceContains");
            SerializedProperty controlContains = binding.FindPropertyRelative("controlContains");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(controlPath);
                EditorGUILayout.PropertyField(deviceContains);
                EditorGUILayout.PropertyField(controlContains);
            }

            EditorGUILayout.BeginHorizontal();

            bool isListeningHere = listenTarget == target && listenBindingIndex == i;
            string bindLabel = isListeningHere ? "Listening... Press Controller Button" : "Bind By Pressing Controller Button";

            if (GUILayout.Button(bindLabel))
            {
                serializedObject.ApplyModifiedProperties();
                StartListening((ControllerKeyboardBinder)target, i);
            }

            if (GUILayout.Button("Clear", GUILayout.Width(70)))
            {
                controlPath.stringValue = string.Empty;
                deviceContains.stringValue = string.Empty;
                controlContains.stringValue = string.Empty;
                serializedObject.ApplyModifiedProperties();
                ((ControllerKeyboardBinder)target).RefreshBindings();
                EditorUtility.SetDirty(target);
            }

            if (isListeningHere && GUILayout.Button("Cancel", GUILayout.Width(70)))
            {
                StopListening();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Binding"))
        {
            int index = bindingsProperty.arraySize;
            bindingsProperty.arraySize++;

            SerializedProperty binding = bindingsProperty.GetArrayElementAtIndex(index);
            binding.FindPropertyRelative("label").stringValue = string.Empty;
            binding.FindPropertyRelative("keyboardKey").enumValueIndex = 0;
            binding.FindPropertyRelative("controlPath").stringValue = string.Empty;
            binding.FindPropertyRelative("deviceContains").stringValue = string.Empty;
            binding.FindPropertyRelative("controlContains").stringValue = string.Empty;
        }
    }

    private static void StartListening(ControllerKeyboardBinder binder, int bindingIndex)
    {
        StopListening();

        listenTarget = binder;
        listenBindingIndex = bindingIndex;

        listenSubscription = InputSystem.onAnyButtonPress
            .Where(IsBindableControllerButton)
            .CallOnce(control => CompleteBinding(binder, bindingIndex, control));

        Debug.Log(
            $"[{nameof(ControllerKeyboardBinder)}] Listening for controller button for binding {bindingIndex}.",
            binder);
    }

    private static void StopListening()
    {
        listenSubscription?.Dispose();
        listenSubscription = null;
        listenTarget = null;
        listenBindingIndex = -1;
    }

    private static bool IsBindableControllerButton(InputControl control)
    {
        if (!(control is ButtonControl))
        {
            return false;
        }

        InputDevice device = control.device;
        return !(device is Keyboard) &&
               !(device is Mouse) &&
               !(device is Pointer);
    }

    private static void CompleteBinding(ControllerKeyboardBinder binder, int bindingIndex, InputControl control)
    {
        if (binder == null)
        {
            StopListening();
            return;
        }

        SerializedObject serializedBinder = new SerializedObject(binder);
        SerializedProperty bindings = serializedBinder.FindProperty("bindings");

        if (bindingIndex < 0 || bindingIndex >= bindings.arraySize)
        {
            StopListening();
            return;
        }

        SerializedProperty binding = bindings.GetArrayElementAtIndex(bindingIndex);
        binding.FindPropertyRelative("controlPath").stringValue = control.path;
        binding.FindPropertyRelative("deviceContains").stringValue = GetBestDeviceFilter(control.device);
        binding.FindPropertyRelative("controlContains").stringValue = control.name;

        serializedBinder.ApplyModifiedProperties();
        EditorUtility.SetDirty(binder);
        binder.RefreshBindings();

        Debug.Log(
            $"[{nameof(ControllerKeyboardBinder)}] Bound '{control.displayName}' on '{control.device.displayName}' " +
            $"to binding {bindingIndex}. Path: {control.path}",
            binder);

        StopListening();
    }

    private static string GetBestDeviceFilter(InputDevice device)
    {
        if (!string.IsNullOrWhiteSpace(device.displayName))
        {
            return device.displayName;
        }

        if (!string.IsNullOrWhiteSpace(device.description.product))
        {
            return device.description.product;
        }

        return device.layout;
    }
}
