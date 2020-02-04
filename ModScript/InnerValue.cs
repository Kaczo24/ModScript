using System;
using System.Collections.Generic;

namespace ModScript
{
    static class InnerValue
    {
        public static Value length(Value v)
        {
            if (v.type == "STRING")
                return new Value(v.text.Length);
            if (v.type == "LIST")
                return new Value(v.values.Count);
            return Value.NULL;
        }
    }
}
