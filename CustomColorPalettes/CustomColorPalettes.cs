using System.Linq;
using HarmonyLib;
using TCFDS.Helpers;
using UnityEngine;

namespace TCFDS.CustomColorPalettes;

public static class CustomColorPalettes
{
	private static ColorSet[] AllPalettes = [
		// Test:
		new ColorSet {
			quality = 1f,
			intensity = 1f,
			t_btn1 = ColorHelpers.FromHex(0x3035CD),
			t_btn2 = ColorHelpers.FromHex(0xDAAC94),
			t_btn3 = ColorHelpers.FromHex(0xF75B92),
			t_btn4 = ColorHelpers.FromHex(0xDEE46C),
			t_btn5 = ColorHelpers.FromHex(0x6D6A61),
			t_btn6 = ColorHelpers.FromHex(0x2B94A8),
			oh_base = ColorHelpers.FromHex(0xFFFFFF),
			oh_correct = ColorHelpers.FromHex(0x98F530),
			oh_incorrect = ColorHelpers.FromHex(0xDA422A),
			inf_bg = ColorHelpers.FromHex(0x140D03),
			inf_bg2 = ColorHelpers.FromHex(0x0D0802),
			inf_tab = ColorHelpers.FromHex(0x56360E),
			inf_brightBtn = ColorHelpers.FromHex(0xB18CFF),
			inf_brightBtnPressed = ColorHelpers.FromHex(0x5A3C99),
			inf_brightBtnHover = ColorHelpers.FromHex(0xE5D9FF),
			inf_darkBtn = ColorHelpers.FromHex(0x5B1A1A),
			inf_darkBtnPressed = ColorHelpers.FromHex(0x310C0D),
			inf_darkBtnHover = ColorHelpers.FromHex(0xC74244),
			inf_brightTxt = ColorHelpers.FromHex(0xF0F0F0),
			inf_darkTxt = ColorHelpers.FromHex(0x000000),
			ph_backplate = ColorHelpers.FromHex(0x937786),
			ph_backwall = ColorHelpers.FromHex(0x3B0F1F),
			ph_vent = ColorHelpers.FromHex(0x71314C),
			ph_mic = ColorHelpers.FromHex(0x86446a),
		}
	];

	private static int customPalettesOffset;

	[HarmonyPatch(typeof(ColorManager), nameof(ColorManager.InitialCheck))]
	[HarmonyPostfix]
	public static void AddCustomColorPalettes(ColorManager __instance) {
		customPalettesOffset = __instance.colSets.Length;
		__instance.colSets = __instance.colSets.Concat([]).ToArray();
	}

	public static int GetIndexForPalette(CustomPalettes palette) {
		return customPalettesOffset + (int)palette;
	}
}

public enum CustomPalettes
{
	Test = 0,
}
