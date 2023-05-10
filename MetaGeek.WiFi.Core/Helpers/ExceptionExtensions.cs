using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaGeek.WiFi.Core.Helpers
{
    public static class ExceptionExtensions
    {
        public static void ThrowIfNull(this object obj, string arg)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(arg);
            }
        }

        public static void ThrowIfUninitialized<T>(this T obj, string arg)
        {
            if (obj.Equals(default(T)))
            {
                throw new ArgumentNullException(arg);
            }
        }
    }
}
