using UnityEngine;

namespace TCFDS.Helpers;

public static class ColorHelpers
{
	public static Color FromHex(int hex) {
		int r = (hex >> 16) & 0xFF;
		int g = (hex >> 8) & 0xFF;
		int b = hex & 0xFF;
		return new Color(r / 255f, g / 255f, b / 255f, 1f);
	}
}
