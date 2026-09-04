using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using TCFDS.CustomColorPalettes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TCFDS;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class TCFDSPlugin : BaseUnityPlugin
{
	public static ConfigEntry<ColorSetType> VanillaColorPalette;
	//public static ConfigEntry<ModCustomPalettes> ModCustomColorPalette;
	public static ConfigEntry<ColorRandomizationType> RandomizationType;
	public static ConfigEntry<float> RandomizationDelay;
	public static ConfigEntry<float> KeyPressDebounce;
	public static ConfigEntry<string> RandomizationBlocklist;
	public static ConfigEntry<bool> ForceMulticoloredConfetti;
	public static new ManualLogSource Logger;

	private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

	private void Awake() {
		Logger = base.Logger;
		Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

		harmony.PatchAll();

		string section;
		string key;
		string description;

		section = "Colors";
		key = "VanillaColorPalette";
		description = "The vanilla theme to use. Random sets the colour to a random one on load. None keeps the color how it is";
		VanillaColorPalette = Config.Bind(section, key, ColorSetType.None, description);

		/*
		key = "ModCustomColorPalette";
		description = "The mod-defined custom theme to use. Only applies if VanillaColorPalette is set to ModCustom";
		ModCustomColorPalette = Config.Bind(section, key, ModCustomPalettes.None, description);
		*/

		key = "ForceMulticoloredConfetti";
		description = "Forces confetti to be multicolored";
		ForceMulticoloredConfetti = Config.Bind(section, key, true, description);

		section = "Randomization";
		key = "RandomizationType";
		description = "How often to randomize colors. Only applies if VanillaColorPalette is set to Random\nOnLoad = Randomizes when the game is loaded\nOnPuzzleComplete = Randomizes each time you complete a puzzle\nOnWeekComplete = Randomizes when you complete a week (in-game)\nOnDelay = Randomizes every number of seconds, set by RandomizationDelay\nOnKeyPress = ** WARNING, DO NOT USE IF YOU ARE SENSITIVE TO FLASHING LIGHTS ** Randomizes whenever you press a key (yes, seriously)";
		RandomizationType = Config.Bind(section, key, ColorRandomizationType.OnLoad, description);

		key = "RandomizationDelay";
		description = "The delay, in human seconds, between color randomization. Only applies if RandomizationType is set to OnDelay";
		RandomizationDelay = Config.Bind(section, key, 60f, description);

		key = "KeyPressDebounce";
		description = "How long in milliseconds to wait after a keypress to change the color palette. Only applies if RandomizationType is set to OnKeyPress\nLarger values will be less seamless, but can fix issues with lag if you are having them";
		KeyPressDebounce = Config.Bind(section, key, 1f, description);

		key = "RandomizationBlocklist";
		description = "A comma-separated list of color palettes to ignore when randomizing.\nNote: Ignores case, must keep at least 2 color palettes allowed\nExample: \"YellowGreen, OrangePurple, White\"";
		RandomizationBlocklist = Config.Bind(section, key, "", description);
	}
}

[HarmonyPatch]
public static class ColorManagerPatch
{
	private static ColorManager colorManager;
	private static List<int> allowedColors = new List<int>();
	private static int randomColor;

	private static Coroutine updateColorsRoutine;
	private static float timeSinceRandomizationByDelay = 0f;

	[HarmonyPatch(typeof(ColorManager), nameof(ColorManager.ClearedID))]
	[HarmonyPrefix]
	public static void SetColorsByConfig(ColorManager __instance, ref int id) {
		colorManager = __instance;
		if (TCFDSPlugin.VanillaColorPalette.Value == ColorSetType.None) {
			return;
		}

		int chosenColors = (int)TCFDSPlugin.VanillaColorPalette.Value;
		if (TCFDSPlugin.VanillaColorPalette.Value == ColorSetType.Random) {
			chosenColors = randomColor;
		}

		/*
		if (TCFDSPlugin.VanillaColorPalette.Value == ColorSetType.ModCustom) {
			chosenColors = ModCustomColorPalettes.GetIndexForPalette(TCFDSPlugin.ModCustomColorPalette.Value);
			TCFDSPlugin.Logger.LogInfo("Custom");
			//TCFDSPlugin.Logger.LogInfo("Setting mod custom palette: " + Enum.GetName(typeof(ModCustomColorPalettes), TCFDSPlugin.ModCustomColorPalette.Value));
		}
		*/

		id = chosenColors;
	}

	[HarmonyPatch(typeof(PuzzleManager), nameof(PuzzleManager.LoadNextPuzzle))]
	[HarmonyPostfix]
	public static void RandomizeOnPuzzleComplete(PuzzleManager __instance) {
		if (TCFDSPlugin.VanillaColorPalette.Value != ColorSetType.Random) {
			return;
		}

		if (TCFDSPlugin.RandomizationType.Value == ColorRandomizationType.OnPuzzleComplete) {
			randomColor = GetRandomColor(__instance.totalPuzzleID);
			UpdateColorsWithRandomOnes();
		}
	}

	[HarmonyPatch(typeof(WeekScreen), nameof(WeekScreen.ClearedID))]
	[HarmonyPostfix]
	public static void RandomizeOnWeekComplete(int id, int prevExceeded) {
		if (TCFDSPlugin.VanillaColorPalette.Value != ColorSetType.Random) {
			return;
		}

		if (TCFDSPlugin.RandomizationType.Value == ColorRandomizationType.OnWeekComplete && id != prevExceeded) {
			randomColor = GetRandomColor(id);
			UpdateColorsWithRandomOnes();
		}
	}

	[HarmonyPatch(typeof(HotkeyManager), nameof(HotkeyManager.Update))]
	[HarmonyPostfix]
	public static void RandomizeOnDelay() {
		if (TCFDSPlugin.VanillaColorPalette.Value != ColorSetType.Random) {
			return;
		}

		if (TCFDSPlugin.RandomizationType.Value == ColorRandomizationType.OnDelay && timeSinceRandomizationByDelay <= 0f) {
			randomColor = GetRandomColor();
			UpdateColorsWithRandomOnes();
			timeSinceRandomizationByDelay = TCFDSPlugin.RandomizationDelay.Value;
		}

		timeSinceRandomizationByDelay -= Time.deltaTime;
	}

	[HarmonyPatch(typeof(HotkeyManager), nameof(HotkeyManager.Update))]
	[HarmonyPostfix]
	public static void RandomizeOnKeypress() {
		if (TCFDSPlugin.VanillaColorPalette.Value != ColorSetType.Random) {
			return;
		}

		if (TCFDSPlugin.RandomizationType.Value == ColorRandomizationType.OnKeyPress && Input.anyKeyDown) {
			if (updateColorsRoutine != null) {
				colorManager.StopCoroutine(updateColorsRoutine);
			}

			updateColorsRoutine = colorManager.StartCoroutine(UpdateColorsDebounced());
		}
	}

	[HarmonyPatch(typeof(DictionaryWindow), nameof(DictionaryWindow.DrawDictionary))]
	[HarmonyPostfix]
	public static void InitRandomColors() {
		if (TCFDSPlugin.VanillaColorPalette.Value != ColorSetType.Random) {
			return;
		}

		List<int> randomBlocklist = new List<int>();
		var randomBlocklistStrings = TCFDSPlugin.RandomizationBlocklist.Value
			.Trim(' ')
			.Split(',')
			.ToArray();

		foreach (var str in randomBlocklistStrings) {
			if (Enum.TryParse<ColorSetType>(str, true, out ColorSetType colorSetToBlock)) {
				randomBlocklist.Add((int)colorSetToBlock);
			} else {
				TCFDSPlugin.Logger.LogError("Could not find color palette '" + str + "', skipping");
			}
		}

		var allColors = Enum.GetValues(typeof(ColorSetType))
			.Cast<ColorSetType>()
			.Where(x => x != ColorSetType.None && x != ColorSetType.Random)
			.Select(x => (int)x);
		allowedColors = allColors.Where(x => !randomBlocklist.Contains(x)).ToList();

		randomColor = TCFDSPlugin.RandomizationType.Value switch {
			ColorRandomizationType.OnPuzzleComplete => GetRandomColor(PuzzleManager.Instance.totalPuzzleID),
			_ => GetRandomColor()
		};
	}

	[HarmonyPatch(typeof(ConfettiCannon), nameof(ConfettiCannon.FireConfetti))]
	[HarmonyPrefix]
	public static void ChangeConfettiColor(ConfettiCannon __instance) {
		if (TCFDSPlugin.ForceMulticoloredConfetti.Value) {
			__instance.ConfettiMode = 1;
		}
	}

	private static int GetRandomColor(int? tempState = null) {
		var currState = Random.state;
		if (tempState.HasValue) {
			Random.InitState(tempState.Value);
		}

		int result;
		int infiniteRecursionBlocker = 0;
		do {
			result = allowedColors[Random.RandomRangeInt(0, allowedColors.Count)];
			infiniteRecursionBlocker++;

			if (infiniteRecursionBlocker > 100) {
				break;
			}
		} while (result == randomColor);
		
		if (tempState.HasValue) {
			Random.state = currState;
		}

		return result;
	}

	private static void UpdateColorsWithRandomOnes() {
		if (TCFDSPlugin.VanillaColorPalette.Value != ColorSetType.Random) {
			return;
		}

		ColorManager.currSet = colorManager.colSets[randomColor];
		colorManager.BroadcastColorSet();
	}

	private static IEnumerator UpdateColorsDebounced() {
		yield return new WaitForSeconds(TCFDSPlugin.KeyPressDebounce.Value / 1000f);
		randomColor = GetRandomColor();
		UpdateColorsWithRandomOnes();
	}
}

public enum ColorSetType : byte
{
	None = 0,
	PinkBrown,
	Blue,
	Purple,
	YellowGreen,
	YellowRed,
	PinkRed,
	Cyan,
	OrangePurple,
	White,
	RetroGreen,
	Random,
//	ModCustom,
}

public enum ColorRandomizationType : byte
{
	OnLoad,
	OnPuzzleComplete,
	OnWeekComplete,
	OnDelay,
	OnKeyPress,
}
