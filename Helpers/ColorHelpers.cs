using UnityEngine;

namespace TCFDS.Helpers;

public static class ColorHelpers
{
	public static Color FromHex(int hex) {
		int r = (hex >> 16) & 0xFF;
		int g = (hex >> 8) & 0xFF;
		int b = hex & 0xFF;
		return new Color(r, g, b, 255);
	}
}
