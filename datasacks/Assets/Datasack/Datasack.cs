﻿/*
    The following license supersedes all notices in the source code.
*/

/*
    Copyright (c) 2018 Kurt Dekker/PLBM Games All rights reserved.

    http://www.twitter.com/kurtdekker
    
    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions are
    met:
    
    Redistributions of source code must retain the above copyright notice,
    this list of conditions and the following disclaimer.
    
    Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
    
    Neither the name of the Kurt Dekker/PLBM Games nor the names of its
    contributors may be used to endorse or promote products derived from
    this software without specific prior written permission.
    
    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS
    IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
    TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
    PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
    HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
    SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
    TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
    PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
    LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
    NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
    SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu]
public class Datasack : ScriptableObject
{
	public	string	InitialValue;

	public	bool	Save;

	public	delegate	void	OnValueChangedDelegate( Datasack ds);

	[NonSerialized]
	public	OnValueChangedDelegate	OnChanged;

	void OnEnable()
	{
		Value = InitialValue;

		if (Save)
		{
			Value = PlayerPrefs.GetString (
				DSM.s_PlayerPrefsPrefix + name.ToLower(), Value);
		}
	}

	[NonSerialized] private	string	TheData;

	public	string	Value
	{
		get
		{
			return TheData;
		}
		set
		{
			TheData = value;
			if (OnChanged != null)
			{
				OnChanged.Invoke (this);
			}
			if (Save)
			{
				#if UNITY_EDITOR
				if (EditorApplication.isPlaying)
				#endif
					DSM.I.SetDirty();
			}
		}
	}

	public	int		iValue
	{
		get
		{
			int i = 0;
			int.TryParse (Value, out i);
			return i;
		}
		set
		{
			Value = value.ToString();
		}
	}

	public	float	fValue
	{
		get
		{
			return DatasackFormatting.FromHexString( Value);
		}
		set
		{
			Value = DatasackFormatting.ToHexString( value);
		}
	}

	// CAUTION: nonzero integer is true... a string "true" doesn't cut it!
	public	bool	bValue
	{
		get
		{
			return iValue != 0;
		}
		set
		{
			iValue = value ? 1 : 0;
		}
	}

	public override string ToString ()
	{
		return string.Format ("[Datasack: name={0}, Value={1}, iValue={2}, fValue={3}, bValue={4}]", name, Value, iValue, fValue, bValue);
	}

	#if UNITY_EDITOR
	[CustomEditor(typeof(Datasack))]
	public class DatasackEditor : Editor
	{
		void AppendGetter( ref string s, Datasack ds)
		{
			string safeName = ds.name;

			// Note to future self: don't allow future silliness by putting non-identifier-safe
			// names into filenames. Tell the user to be thankful we don't enforce 8.3 filenames.

			s += "\tpublic static Datasack " + safeName + " { get { return DSM.I.Get( \"" +
				safeName  + "\"); } }\n";
		}

		void GenerateCode()
		{
			Debug.Log( "CODEGEN!");

			string s = "//\n//\n//\n" + 
				"// MACHINE-GENERATED CODE - DO NOT MODIFY BY HAND!\n" +
				"//\n//\n" +
				"// To regenerate this file, select a Datasack object, look\n" +
				"// in the custom Inspector window and press the CODEGEN button.\n" +
				"//\n//\n//\n";

			s += "public partial class DSM\n{\n";

			Datasack[] sacks = Resources.LoadAll<Datasack>( "Datasacks/");
			foreach( var ds in sacks)
			{
				AppendGetter( ref s, ds);
			}

			s += "}\n";

			Debug.Log( s);

			string outfile = "Assets/Datasack/DSMCodegen.cs";
			using( System.IO.StreamWriter sw =
				new System.IO.StreamWriter(outfile, false))
			{
				sw.Write(s);
			}

			AssetDatabase.Refresh();
		}

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			EditorGUILayout.BeginVertical();

			if (GUILayout.Button( "CODEGEN"))
			{
				GenerateCode();
			}

			EditorGUILayout.EndVertical();
		}
	}
	#endif
}
