﻿/*
	The following license supersedes all notices in the source code.

	Copyright (c) 2019 Kurt Dekker/PLBM Games All rights reserved.

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

public partial class Datasack
{
	public override string ToString ()
	{
		return string.Format ("[Datasack: name={0}, FullName={1}, Value={2}, iValue={3}, fValue={4}, bValue={5}]",
			name, FullName, Value, iValue, fValue, bValue);
	}

	bool DebugBreak;
	bool DebugLogging;

#if UNITY_EDITOR
	[CustomEditor(typeof(Datasack)), CanEditMultipleObjects]
	public class DatasackEditor : Editor
	{
		string IdentifierSafeString( string s)
		{
			s = s.Replace( "_", "_");
			s = s.Replace( "-", "_");
			s = s.Replace( "/", "_");
			s = s.Replace( "\\", "_");
			return s;
		}

		void CreateStaticGetterExpression( ref string s, Datasack ds, string variableName)
		{
			s += "\tpublic static Datasack " + IdentifierSafeString( ds.name) +
				" { get { return DSM.I.Get( \"" +
				variableName  + "\", Load: true); } }\n";
		}

		void GenerateCode()
		{
			Debug.Log( "CODEGEN!");

			string s = "//\n//\n//\n" + 
				"// MACHINE-GENERATED CODE - DO NOT MODIFY BY HAND!\n" +
				"//\n//\n" +
				"// To regenerate this file, select any Datasack object, look\n" +
				"// in the custom Inspector window and press the CODEGEN button.\n" +
				"//\n//\n//\n";

			s += "public partial class DSM\n{\n";

			// TODO: make this ransack the entire project for Datasacks,
			// not just underneath the Resources directories!

			Datasack[] sacks = Resources.LoadAll<Datasack>( DSM.s_DatasacksDirectoryPrefix);

			Dictionary<string,List<Datasack>> SplitByDirectory = new Dictionary<string, List<Datasack>>();

			foreach( var ds in sacks)
			{
				string assetPath = AssetDatabase.GetAssetPath( ds.GetInstanceID());
				string dirName = System.IO.Path.GetDirectoryName( assetPath);
				if (!SplitByDirectory.ContainsKey( dirName))
				{
					SplitByDirectory[dirName] = new List<Datasack>();
				}
				SplitByDirectory[dirName].Add( ds);
			}

			foreach( var dirName in SplitByDirectory.Keys)
			{
				string nestedClassName = null;
				string pathPrefix = "";
				string indentation = "";

				const string s_DatasacksSearchTarget = "/Datasacks";

				s += "\n";
				s += "// Datasacks from directory '" + dirName + "'\n";

				// CAUTION: this only handles "nested namespaces"
				// one class (folder) deep for now. Feel free to improve it
				// and send me a pull request!
				if (!dirName.EndsWith( s_DatasacksSearchTarget))
				{
					int resourcesOffset = dirName.LastIndexOf( s_DatasacksSearchTarget);

					resourcesOffset += s_DatasacksSearchTarget.Length + 1;

					nestedClassName = dirName.Substring( resourcesOffset);

					pathPrefix = nestedClassName + "/";

					indentation = "\t";
				}

				if (nestedClassName != null)
				{
					s += "\tpublic static class " + IdentifierSafeString( nestedClassName) + "\n";
					s += "\t{\n";
				}

				foreach( var ds in SplitByDirectory[dirName])
				{
					s += indentation;
					string variableName = pathPrefix + ds.name;
					CreateStaticGetterExpression( ref s, ds, variableName);
					ds.FullName = variableName;
				}

				if (nestedClassName != null)
				{
					s += "\t}\n";
				}
			}

			s += "}\n";

			Debug.Log( s);

			string outfile = "Assets/Datasack/DSMCodegen.cs";
			using( System.IO.StreamWriter sw =
				new System.IO.StreamWriter(outfile, false))
			{
				sw.Write(s);
			}

			{
				bool create = false;

				string assetPath = "Assets/" + DSM.s_AllDatasacksPathPrefix;

				System.IO.Directory.CreateDirectory( assetPath);

				assetPath = assetPath + DSM.s_AllDatasacksAsset + ".asset";

				var dsc = AssetDatabase.LoadAssetAtPath<DatasackCollection>( assetPath);
				if (!dsc)
				{
					create = true;
					dsc = ScriptableObject.CreateInstance<DatasackCollection>();
				}

				dsc.Mappings = new DatasackCollection.DatasackMapping[ sacks.Length];

				for (int i = 0; i < sacks.Length; i++)
				{
					dsc.Mappings[i] = new DatasackCollection.DatasackMapping();
					dsc.Mappings[i].Fullname = sacks[i].FullName;
					dsc.Mappings[i].Datasack = sacks[i];
				}

				if (create)
				{
					AssetDatabase.CreateAsset( dsc, assetPath);
				}

				EditorUtility.SetDirty(dsc);

				AssetDatabase.SaveAssets();
			}

			AssetDatabase.Refresh();
		}

		string PlayerPrefsKey()
		{
			Datasack ds = (Datasack)target;
			return DSM.s_PlayerPrefsPrefix + ds.FullName;
		}

		public override void OnInspectorGUI()
		{
			Datasack ds = (Datasack)target;

			DrawDefaultInspector();

			EditorGUILayout.BeginVertical();

			if (GUILayout.Button( "CODEGEN"))
			{
				GenerateCode();
			}

			GUILayout.Space(20);

			if (GUILayout.Button( "RUNTIME POKE"))
			{
				ds.Poke();
			}

			GUILayout.Space(20);

			GUI.color = Color.green;
			if (GUILayout.Button( "RESET TO INITIAL VALUE"))
			{
				ds.Value = ds.InitialValue;
			}

			GUILayout.Space(20);

			GUI.color = Color.cyan;
			if (GUILayout.Button( "OUTPUT CURRENT VALUE"))
			{
				string part1 = "Datasack " + ds.FullName + " is currently: '" + ds.Value + "'";
				string part2 = " <not parseable as float>";
				try
				{
					part2 = "--> as float value = " + ds.fValue;
				}
				catch { } 		// gotta catch 'em all: fairly harmless in a small context
				Debug.Log( part1 + part2);
			}

			GUILayout.Space(20);

			GUI.color = Color.yellow;
			if (GUILayout.Button( "DELETE SAVED VALUE"))
			{
				if (PlayerPrefs.HasKey(PlayerPrefsKey()))
				{
					PlayerPrefs.DeleteKey( PlayerPrefsKey());
					PlayerPrefs.Save();
				}
			}

			GUILayout.Space(20);

			GUI.color = Color.red;
			if (GUILayout.Button( "DELETE ALL PLAYER PREFS"))
			{
				DSM.ResetDictionaryIfRunning();
				PlayerPrefs.DeleteAll();
				PlayerPrefs.Save();
			}

			GUILayout.Space(20);

			GUI.color = ds.DebugBreak ? Color.white : Color.gray;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Debug Break", GUILayout.Width(160));
			ds.DebugBreak = GUILayout.Toggle( ds.DebugBreak, "BREAK");
			GUILayout.EndHorizontal();

			GUI.color = ds.DebugLogging ? Color.white : Color.gray;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Debug Logging", GUILayout.Width(160));
			ds.DebugLogging = GUILayout.Toggle( ds.DebugLogging, "LOG");
			GUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();
		}
	}
#endif
}
