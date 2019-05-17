using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities
{
    class ConfigHelper
    {
        public static float ConfigToFloat(string configLine)
        {
            if (float.TryParse(configLine, out var x))
            {
                return x;
            }
            return 0.0f;
        }
    }
}
