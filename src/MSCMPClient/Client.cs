using System.IO;
using System.Reflection;
using System.Diagnostics;
using UnityEngine;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace MSCO
{
	/// <summary>
	/// Main class of the mod.
	/// </summary>
	public class Client {

		/// <summary>
		/// Asset bundle containing multiplayer mod content.
		/// </summary>
		static AssetBundle assetBundle = null;

		/// <summary>
		/// The my summer car game app id.
		/// </summary>
		public static readonly Steamworks.AppId_t GAME_APP_ID = new Steamworks.AppId_t(516750);

		/// <summary>
		/// Command line dictionary - key is param, value is value (can be empty)
		/// </summary>
		public static Dictionary<string, string> commandLineDict = new Dictionary<string, string>();

		/// <summary>
		/// Starts the mod. Called from Injector.
		/// </summary>
		public static void Start(string commandLine) {
			if (!SetupLogger()) {
				return;
			}

			ParseCommandLine(commandLine);

			Logger.SetAutoFlush(true);

			Game.Hooks.PlayMakerActionHooks.Install();

			string assetBundlePath = GetPath("../../data/mpdata");
			if (!File.Exists(assetBundlePath)) {
				FatalError("Cannot find mpdata asset bundle.");
				return;
			}

			assetBundle = AssetBundle.CreateFromFile(assetBundlePath);

			var go = new GameObject("Multiplayer GUI Controller");
			go.AddComponent<UI.MPGUI>();

			go = new GameObject("Multiplayer Controller");
			go.AddComponent<MPController>();

			UI.Console.RegisterCommand("quit", (string[] args) => {
				Application.Quit();
			});
		}

		/// <summary>
		/// Gets absolute path for the specified file relative to mod installation folder.
		/// </summary>
		/// <param name="file">The file to get path for.</param>
		/// <returns>Absolute path for the specified file relative to mod instalation folder.</returns>
		public static string GetPath(string file) {
			return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\" + file;
		}

		/// <summary>
		/// Loads asset from multiplayer mod asset bundle.
		/// </summary>
		/// <typeparam name="T">The type of the asset to load.</typeparam>
		/// <param name="name">The name of the asset to load.</param>
		/// <returns>Loaded asset.</returns>
		public static T LoadAsset<T>(string name) where T : UnityEngine.Object {
			return assetBundle.LoadAsset<T>(name);
		}

		/// <summary>
		/// Call this when fatal error occurs. This will print error into the log and close the game.
		/// </summary>
		/// <param name="message">The message to print to console.</param>
		public static void FatalError(string message) {
			Logger.Log(message);
			Logger.Log(Environment.StackTrace);
			ShowMessageBox(message, "MSCO - Fatal error");

#if DEBUG
			if (Debugger.IsAttached) {
				throw new Exception(message);
			}
			else {
#endif
				Process.GetCurrentProcess().Kill();
#if DEBUG
			}
#endif
		}

		/// <summary>
		/// Standard assertion. If given condition is not true then prints message to the log and closes game.
		/// </summary>
		/// <param name="condition">Condition to chec.</param>
		/// <param name="message">The message to print to console.</param>
		public static void Assert(bool condition, string message) {
			if (condition) {
				return;
			}
			Logger.Log("[ASSERTION FAILED]");
			FatalError(message);
		}

		/// <summary>
		/// Shows system message box to the user. Should be used only during initialization when no ui can be shown in game.
		/// </summary>
		/// <param name="message">The message to show.</param>
		/// <param name="title">The title of the message box.</param>
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static void ShowMessageBox(string message, string title);

		/// <summary>
		/// The current mod development stage.
		/// </summary>
		public const string MOD_DEVELOPMENT_STAGE = "Beta";

		/// <summary>
		/// Get display version of the mod.
		/// </summary>
		/// <returns></returns>
		public static string GetMODDisplayVersion() {
			string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
			version += " " + MOD_DEVELOPMENT_STAGE;
			return version;
		}

		/// <summary>
		/// Add message to the console.
		/// </summary>
		/// <param name="message">The message to add.</param>
		static public void ConsoleMessage(string message) {
			if (UI.Console.Instance != null) {
				UI.Console.Instance.AddMessage(message);
			}
		}

		/// <summary>
		/// Initializes logger.
		/// </summary>
		/// <returns>true if logger initialization has succeeded, false otherwise</returns>
		static private bool SetupLogger() {
			string logPath;

			// First try create clientLog in app data.

			string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string MSCOData = appData + "/MSCO";
			bool MSCODataExists = Directory.Exists(MSCOData);
			if (!MSCODataExists) {
				try {
					MSCODataExists = Directory.CreateDirectory(MSCOData).Exists;
				}
				catch {
					// Nothing.. let us fallback below.
				}
			}

			if (MSCODataExists) {
				logPath = MSCOData + "/clientLog.txt";
				if (Logger.SetupLogger(logPath)) {
					return true;
				}
			}

			// The last chance, setup logger next to the .exe.

			logPath = GetPath("clientLog.txt");
			if (!Logger.SetupLogger(logPath)) {
				FatalError($"Cannot create log file. Log file path: {logPath}\n\nTry running game as administrator.");
				return false;
			}

			return true;
		}

		enum ParserState
		{
			EatingTillKey, EatingTillValue, ReadingKey, ReadingValue,
		}

		/// <summary>
		/// Parses given command line and adds parsed parameters to commandLine param.
		/// </summary>
		/// <param name="commandLine"></param>
		static private void ParseCommandLine(string commandLine)
		{
			if (commandLine.Length == 0) {
				return;
			}

			int parsingIndex = 0;

			// Command line starts from quotes - path. Skip it.
			if (commandLine[0] == '"') {
				parsingIndex = commandLine.IndexOf('"', 1) + 1;
			}

			ParserState state = ParserState.EatingTillKey;
			string buffer = string.Empty;
			string currentParam = string.Empty;

			Action registerParam = () => {
				currentParam = buffer;
				commandLineDict[currentParam] = "";
				buffer = "";
			};
			Action registerValue = () => {
				commandLineDict[currentParam] = buffer.TrimEnd(' ');
				currentParam = "";
				buffer = "";
			};
			for (; parsingIndex<commandLine.Length; ++parsingIndex) {

				char currentCharacter = commandLine[parsingIndex];
				switch (state) {
					case ParserState.EatingTillKey:
					case ParserState.EatingTillValue:
						// If - is found we are reading new key.

						if (currentCharacter == '-') {
							state = ParserState.ReadingKey;
							buffer += currentCharacter;
							break;
						}

						// If space is found we are reading value - only if there is was key before.

						if ((state == ParserState.EatingTillValue) && (currentCharacter != ' ')) {
							state = ParserState.ReadingValue;
							buffer += currentCharacter;
							break;
						}
						break;
					case ParserState.ReadingKey:
						// Read key till space is found

						if (currentCharacter == ' ') {
							registerParam();
							state = ParserState.EatingTillValue;
							break;
						}

						buffer += currentCharacter;
						break;

					case ParserState.ReadingValue:
						// Super simple value reading, we read until end of string or dash for another option is here.

						if (currentCharacter == '-') {
							registerValue();
							state = ParserState.ReadingKey;
							buffer += currentCharacter;
							break;
						}

						buffer += currentCharacter;
						break;
				}
			}

			// Finalize parser state - end of string case

			switch (state) {
				case ParserState.ReadingKey:
					registerParam();
					break;

				case ParserState.ReadingValue:
					registerValue();
					break;
			}
		}

		/// <summary>
		/// Gets command line parameter value.
		/// </summary>
		/// <param name="param">Name of the parameter with the dash prefix</param>
		/// <param name="value">String where value should be append</param>
		/// <returns>true if parameter is set, false otherwise</returns>
		static private bool GetCdlParamValue(string param, ref string value)
		{
			if (commandLineDict.ContainsKey(param)) {
				value = commandLineDict[param];
				return true;
			}
			return false;
		}

		/// <summary>
		/// Check if given command line parameter is set.
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		static public bool IsCdlParamSet(string param)
		{
			return commandLineDict.ContainsKey(param);
		}
	}
}
