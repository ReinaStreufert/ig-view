﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ig_view
{
    public static class Formatting
    {
        public static string PadWhitespace(string line, int colCount)
        {
            var whitespaceCount = colCount - line.Length;
            if (whitespaceCount < 0)
                return line.Substring(0, colCount);
            return line + new string(' ', whitespaceCount);
        }

        public static string PadBetween(string left, string right, int colCount)
        {
            var whitespaceCount = colCount - left.Length - right.Length;
            if (whitespaceCount < 0)
                return $"{left} {right}".Substring(0, colCount);
            return left + new string(' ', whitespaceCount) + right;
        }

        public static string DecipherInstagramness(string instagramAfflictedJson) // my solution is to re-encode before loading json
        {
            // \uXXXX usually refers to a singular utf-16 character. so an apostrophe would be \u0027
            // in text affected by instagram's apparently poorly written backend, these are utf-8 bytes
            // where they only actually firstly need 2 hex digits to be represented, not 4, and multiple bytes
            // utf-8 bytes encode many characters, it is variable length, and they are meant to be expressed
            // contiguously.its weird and technically ambiguous to write a utf-8 sequence as multiple seperate escape characters
            var sb = new StringBuilder();
            List<byte> bufferedUtf8Bytes = new List<byte>();
            for (int i = 0; i < instagramAfflictedJson.Length; i++)
            {
                var c = instagramAfflictedJson[i];
                if ((instagramAfflictedJson.Length - i) >= 6 && c == '\\' && instagramAfflictedJson[i + 1] == 'u')
                {
                    var hexCode = instagramAfflictedJson.Substring(i + 2, 4);
                    bufferedUtf8Bytes.Add(Convert.FromHexString(hexCode)[1]);
                    i += 5; // bc the for loop already increments to make this 6. i always fuck that up
                }
                else
                {
                    if (bufferedUtf8Bytes.Count > 0)
                    {
                        var utf8Bytes = bufferedUtf8Bytes.ToArray();
                        bufferedUtf8Bytes.Clear();
                        var symbol = ReplaceIphoneQuotes(Encoding.UTF8.GetString(utf8Bytes));
                        sb.Append(string.Concat(symbol.Select(sc => $"\\u{(int)sc:X4}")));
                    }
                    sb.Append(c);
                }
            }
            if (bufferedUtf8Bytes.Count > 0)
            {
                var utf8Bytes = bufferedUtf8Bytes.ToArray();
                sb.Append(Encoding.UTF8.GetString(utf8Bytes));
            }
            return sb.ToString();
            
        }

        private static string ReplaceIphoneQuotes(string symbol)
        {
            // also have to replace tim apples boujie iphone letters
            return symbol switch
            {
                "‘" => "'",
                "’" => "'",
                "“" => "\"",
                "”" => "\"",
                _ => symbol
            };
        }
    }
}
