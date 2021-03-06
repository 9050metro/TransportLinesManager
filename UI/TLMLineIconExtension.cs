﻿using ColossalFramework.Globalization;
using Klyte.Commons.UI.Sprites;
using System;
using System.Linq;

namespace Klyte.TransportLinesManager.UI
{
    internal static class TLMLineIconExtension
    {


        public static string[] getDropDownOptions(string option0 = null)
        {
            string[] result = Enum.GetValues(typeof(LineIconSpriteNames)).OfType<LineIconSpriteNames>().OrderBy(x => x).Select(x =>
              {
                  return Locale.Get("K45_TLM_LINE_ICON_ENUM", x.ToString());
              }).ToArray();

            if (option0 != null)
            {
                result[0] = option0;
            }
            return result;

        }
    }
}