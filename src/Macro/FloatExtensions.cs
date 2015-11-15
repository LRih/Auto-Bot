using System;

namespace AutoBot
{
    public static class FloatExtentions
    {
        public static float GetDecimal(this float e)
        {
            return e - (float)Math.Round(e);
        }
    }
}
