using System;
using System.Collections.Generic;
using System.Linq;

namespace Tagging
{
    public static class StringExtensions
    {
        public static string ToUsersList(this List<string> self)
        {
            if (self.Count == 1)
                return self[0];
            else
                return $"{String.Join(", ", self.Take(self.Count - 1))} and {self.Last()}";
        }
    }
}
