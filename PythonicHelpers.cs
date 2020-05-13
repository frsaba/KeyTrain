using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Pythonic
{
    //TODO: IEnumerable-ify
    static class ListHelpers
    {
        public static List<T> ConcatToList<T>(params object[] toConcat)
        {
            List<T> result = new List<T>();
            foreach (var obj in toConcat)
            {
                if (obj.GetType() == typeof(List<T>))
                {
                    result.AddRange((List<T>)obj);
                }
                else if (typeof(T).IsAssignableFrom(obj.GetType()))
                {
                    result.Add((T)obj);
                }
                else
                {
                    throw new Exception("Unrecognized type in ConcatAll");
                }
            }

            return result;
        }
        public class DefaultDict<TKey, TValue> : Dictionary<TKey, TValue> where TValue : new()
        {
            public new TValue this[TKey key]
            {
                get
                {
                    TValue val;
                    if (!TryGetValue(key, out val))
                    {
                        val = new TValue();
                        Add(key, val);
                    }
                    return val;
                }
                set { base[key] = value; }
            }
        }
    }    
}
