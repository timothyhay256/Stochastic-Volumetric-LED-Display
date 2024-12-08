/*
SimpleSpectrumEditor.cs - Part of Simple Spectrum V2.1 by Sam Boyer.
*/
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;

[CustomEditor(typeof(SimpleSpectrum))]
[CanEditMultipleObjects]
public class SimpleSpectrumEditor : Editor {

    [SerializeField]
    AudioMixerGroup muteGroup;
    
	SerializedProperty  propertyWindow;

    SerializedProperty propertyEnabled;

    SerializedProperty propertySourceType;
    SerializedProperty propertyAudioSource;

	SerializedProperty propertyAttackDamp;
	SerializedProperty propertyDecayDamp;

    SerializedProperty propertyNumSamples;
    SerializedProperty propertySampleChannel;
    SerializedProperty propertyUseLogFreq;
    SerializedProperty propertyMultiplyByFreq;

    SerializedProperty propertyFrequencyLimitLow;
    SerializedProperty propertyFrequencyLimitHigh;

    SerializedProperty propertyBarPrefab;
    SerializedProperty propertyBarAmount;
    SerializedProperty propertyBarYScale;
    SerializedProperty propertyBarMinYScale;
    SerializedProperty propertyBarXScale;
    SerializedProperty propertyBarXSpacing;
    SerializedProperty propertyBarCurveAngle;
    SerializedProperty propertyBarXRotation;

	SerializedProperty propertyUseColorGradient;
	SerializedProperty propertyColorMin;
	SerializedProperty propertyColorMax;
    SerializedProperty propertyColorCurve;
    SerializedProperty propertyColorAttackDamp;
    SerializedProperty propertyColorDecayDamp;

	bool foldoutSpectrumOpen = true;
    bool foldoutSamplingOpen = true;
	bool foldoutBarsOpen = true;
	bool foldoutColorsOpen = true;


    /*void NotifyPropertyChanged()
    {
        if (Application.isPlaying)
        {
            Debug.Log("Something's changed, rebuilding spectrum");
            ((SimpleSpectrum)target).RebuildSpectrum();
        }
	}*/

	void OnEnable(){

        if(((SimpleSpectrum)target).muteGroup == null)
        {
            ((SimpleSpectrum)target).muteGroup = muteGroup;
        }

        propertyEnabled = serializedObject.FindProperty("isEnabled");

        propertySourceType = serializedObject.FindProperty("sourceType");
        propertyAudioSource = serializedObject.FindProperty ("audioSource");
		propertyAttackDamp = serializedObject.FindProperty ("attackDamp");
		propertyDecayDamp = serializedObject.FindProperty ("decayDamp");

        propertyNumSamples = serializedObject.FindProperty("numSamples");
        propertySampleChannel = serializedObject.FindProperty("sampleChannel");
        propertyUseLogFreq = serializedObject.FindProperty("useLogarithmicFrequency");
        propertyMultiplyByFreq = serializedObject.FindProperty("multiplyByFrequency");

        propertyFrequencyLimitLow = serializedObject.FindProperty("frequencyLimitLow");
        propertyFrequencyLimitHigh = serializedObject.FindProperty("frequencyLimitHigh");

        propertyWindow = serializedObject.FindProperty("windowUsed");

        propertyBarPrefab = serializedObject.FindProperty("barPrefab");
        propertyBarAmount = serializedObject.FindProperty("barAmount");
        propertyBarYScale = serializedObject.FindProperty("barYScale");
        propertyBarMinYScale = serializedObject.FindProperty("barMinYScale");
        propertyBarXScale = serializedObject.FindProperty("barXScale");
        propertyBarXSpacing = serializedObject.FindProperty("barXSpacing");
        propertyBarCurveAngle = serializedObject.FindProperty("barCurveAngle");
        propertyBarXRotation = serializedObject.FindProperty("barXRotation");

        propertyUseColorGradient = serializedObject.FindProperty("useColorGradient");
        propertyColorMin = serializedObject.FindProperty("colorMin");
        propertyColorMax = serializedObject.FindProperty("colorMax");
        propertyColorCurve = serializedObject.FindProperty("colorValueCurve");
        propertyColorAttackDamp = serializedObject.FindProperty("colorAttackDamp");
        propertyColorDecayDamp = serializedObject.FindProperty("colorDecayDamp");
	}


	public override void OnInspectorGUI(){
		serializedObject.Update();

		EditorGUILayout.LabelField ("A simple audio spectum generator by Sam Boyer.", new GUIStyle{fontSize = 10});

#if UNITY_WEBGL
        EditorGUILayout.LabelField("NOTE: SimpleSpectrum works with WebGL, but only under certain conditions. Check the docs!", new GUIStyle {wordWrap = true });
#endif

        EditorGUILayout.PropertyField(propertyEnabled);

		foldoutSpectrumOpen = EditorGUILayout.Foldout (foldoutSpectrumOpen,"Spectrum Settings");
		if(foldoutSpectrumOpen){

#if UNITY_WEBGL
            EditorGUILayout.LabelField("Only AudioListener can be used with WebGL.", new GUIStyle { wordWrap = true });
#endif
            EditorGUILayout.PropertyField(propertySourceType);

            if (propertySourceType.enumValueIndex == 0){
				EditorGUILayout.PropertyField (propertyAudioSource);
			}

            if(propertySourceType.enumValueIndex == 4)
            {
                EditorGUILayout.LabelField("Use the spectrumInputData property to set your own data. It's probably worth disabling 'Use Logarithmic Frequency'.", new GUIStyle {fontSize = 10, wordWrap = true });
            }

            EditorGUILayout.PropertyField (propertyAttackDamp);
			EditorGUILayout.PropertyField (propertyDecayDamp);


            foldoutSamplingOpen = EditorGUILayout.Foldout (foldoutSamplingOpen,"Sampling Settings");
            if (foldoutSamplingOpen)
            {
#if UNITY_WEBGL
                EditorGUILayout.LabelField("The number of samples used is shared globally.", new GUIStyle { wordWrap = true });
#endif
                EditorGUILayout.PropertyField(propertyNumSamples);
                EditorGUILayout.PropertyField(propertySampleChannel);

                EditorGUILayout.PropertyField(propertyUseLogFreq);

                EditorGUILayout.PropertyField(propertyFrequencyLimitLow);
                EditorGUILayout.PropertyField(propertyFrequencyLimitHigh);


                EditorGUILayout.PropertyField(propertyMultiplyByFreq); 

                EditorGUILayout.PropertyField(propertyWindow);
            }
		}

		foldoutBarsOpen = EditorGUILayout.Foldout (foldoutBarsOpen,"Bar Settings");
		if (foldoutBarsOpen) {
            EditorGUILayout.PropertyField (propertyBarAmount);
			EditorGUILayout.PropertyField (propertyBarPrefab);
            EditorGUILayout.PropertyField (propertyBarYScale);
            EditorGUILayout.PropertyField(propertyBarMinYScale);
            EditorGUILayout.PropertyField(propertyBarXScale);
            EditorGUILayout.PropertyField(propertyBarXSpacing);
            EditorGUILayout.PropertyField(propertyBarXRotation);
            EditorGUILayout.PropertyField(propertyBarCurveAngle, new GUIContent("Spectrum Bend Angle"));
		}

		foldoutColorsOpen = EditorGUILayout.Foldout (foldoutColorsOpen,"Color Settings");
		if (foldoutColorsOpen) {
#if UNITY_WEBGL
            EditorGUILayout.LabelField("Careful when using Color Gradients in WebGL, it can damage performance.", new GUIStyle { wordWrap = true });
#endif
            EditorGUILayout.PropertyField (propertyUseColorGradient);
            
            EditorGUILayout.PropertyField(propertyColorMin, new GUIContent(propertyUseColorGradient.boolValue ? "Minimum Color" : "Color"));

            if (propertyUseColorGradient.boolValue){

                EditorGUILayout.PropertyField(propertyColorMax, new GUIContent("Maximum Color"));
                EditorGUILayout.PropertyField(propertyColorCurve);
                EditorGUILayout.PropertyField(propertyColorAttackDamp);
                EditorGUILayout.PropertyField(propertyColorDecayDamp);
			}
		}

		if(GUILayout.Button("Rebuild Spectrum")){
            Rebuild();
		}

		serializedObject.ApplyModifiedProperties ();
	}

    private void Rebuild()
    {
        if (Application.isPlaying)
        {
            ((SimpleSpectrum)target).RebuildSpectrum();
        }
    }
}
