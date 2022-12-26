using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace eChartUpdate
{
    public static class clsExtension
    {
        #region DateTime
        /// <summary>
        /// DateTime.ToString("yyyyMMdd")
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static string ToString8(this DateTime d)
        {
            return d.ToString("yyyyMMdd");
        }

        /// <summary>
        /// DateTime.ToString("yyyy-MM-dd")
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static string ToString10(this DateTime d)
        {
            return d.ToString("yyyy-MM-dd");
        }
        #endregion

        #region string

        /// <summary>
        /// string.IsNullOrEmpty(s)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNoE(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        /// <summary>
        /// string.IsNullOrEmpty(s)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNotNoE(this string s)
        {
            return !string.IsNullOrEmpty(s);
        }

        public static DateTime ToDateTime8(this string s)
        {
            if (s.Length != 8) return DateTime.MinValue;
            try
            {
                return Convert.ToDateTime($"{s[..4]}-{s[4..6]}-{s[6..]}");
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// 문자열내 한글만 추출
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string OnlyHangul(this string s)
        {
            return Regex.Replace(s, @"[^ㄱ-ㅎ가-힣]", string.Empty);
        }

        /// <summary>
        /// 문자열의 내용을 공백으로 변경
        /// </summary>
        /// <param name="s"></param>
        /// <param name="Chr"></param>
        /// <returns></returns>
        public static string ToRemoveText(this string s, string Chr)
        {
            return s.Replace(Chr, "");
        }

        /// <summary>
        /// 왼쪽에서부터 숫자열까지 문자열 짜름
        /// </summary>
        /// <param name="Txt"></param>
        /// <param name="TxtLen"></param>
        /// <returns></returns>
        public static string Left(this string Txt, int TxtLen)
        {
            string ConvertTxt;
            if (Txt.Length < TxtLen)
            {
                TxtLen = Txt.Length;
            }
            ConvertTxt = Txt[..TxtLen];
            return ConvertTxt;
        }

        /// <summary>
        /// 오른쪽에서 숫자열까지 문자열 짜름
        /// </summary>
        /// <param name="Txt"></param>
        /// <param name="TxtLen"></param>
        /// <returns></returns>
        public static string Right(this string Txt, int TxtLen)
        {
            string ConvertTxt;
            if (Txt.Length < TxtLen)
            {
                TxtLen = Txt.Length;
            }
            ConvertTxt = Txt.Substring(Txt.Length - TxtLen, TxtLen);
            return ConvertTxt;
        }

        /// <summary>
        /// 날짜형식 문자열에 00000000 --> 0000-00-00 변경
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ToDate(this string s)
        {
            if (s.Length != 8) return s;
            try
            {
                return $"{s[..4]}-{s[4..6]}-{s[6..]}";
            }
            catch
            {
                return s;
            }
        }

        /// <summary>
        /// 특수문자 입력 제한
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool SpecialChar(this string s)
        {
            if (Regex.IsMatch(s, @"[~`!@#$%^&*()_\{}[\]|\\;:'""<>,.?/]") == true) //~`!@#$%^&*()_\-+={}[\]|\\;:'""<>,.?/
                return true;
            return false;
        }

        /// <summary>
        /// 문자열을 숫자 더블형으로 변경(실패시 0으로 리턴)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static double ToDouble(this string s)
        {
            if (double.TryParse(s, out double Result))
                return Result;
            else
                return 0;
        }

        public static double ToDouble(this object pStr)
        {
            return pStr.ToText().ToDouble();
        }


        /// <summary>
        /// 문자 크기 비교
        /// </summary>
        public static bool CmpStr(this object str1, string compareChar, string str2)
        {
            return CmpStr(str1.ToText(), compareChar, str2);
        }
        public static bool CmpStr(this string str1, string compareChar, string str2)
        {
            //(문자비교) 문자를 숫자처럼 크기 비교
            //(str1 < str2) = -1
            //(str1 = str2) = 0
            //(str1 > str2) = 1
            bool soRet = false;
            int siRet = string.Compare(str1, str2);

            switch (compareChar)
            {
                case ">":
                    soRet = (siRet == 1);
                    break;
                case ">=":
                    soRet = (siRet == 1 || siRet == 0);
                    break;
                case "=":
                    soRet = (siRet == 0);
                    break;
                case "<":
                    soRet = (siRet == -1);
                    break;
                case "<=":
                    soRet = (siRet == -1 || siRet == 0);
                    break;
            }

            return soRet;
        }

        /// <summary>
        /// 문자열이 데이트형인지 확인
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsDateTime(this string s)
        {
            if (s.IsNoE())
                return false;
            else
                return DateTime.TryParse(s, out _);
        }

        /// <summary>
        /// 문자열에서 널이면 0으로 문자열로 만들기
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string IsZero(this string s)
        {
            if (s.IsNoE())
                return "0";
            else
                return s;
        }

        /// <summary>
        /// 문자열 시계 형태로 표현
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ToTime(this string s)
        {
            if (s.IsNoE())
                return "";
            else
                switch (s.Length)
                {
                    case 4:
                        return s.Left(2) + ":" + s.Right(2);

                    case 6:
                        return s.Left(2) + ":" + s.Substring(2, 2) + ":" + s.Right(2);
                    default:
                        return s;
                }
        }

        /// <summary>
        /// 문자열 첫자가 영어이면 패스, 첫자가 한글이면 영어문자는 제거
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string RemoveAlphabet(this string s)
        {
            string result = string.Empty;
            int i = 0;

            foreach (var ch in s)
            {
                if (i == 0 && !((0xAC00 <= ch && ch <= 0xD7A3) || (0x3131 <= ch && ch <= 0x318E)))
                {
                    result = s;
                    break;
                }
                else
                    i++;

                if ((0xAC00 <= ch && ch <= 0xD7A3) || (0x3131 <= ch && ch <= 0x318E))
                    result += ch;
                else
                    break;
            }
            return result;
        }
        #endregion

        #region object

        /// <summary>
        /// object가 null/DBNull 경우 p 리턴
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static object Nvl<T>(this object o, T p)
        {
            if (o == null)
            {
                return p;
            }
            else if (o == DBNull.Value)
            {
                return p;
            }
            return o;
        }


        /// <summary>
        /// string.Concat(o)
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string ToText(this object o)
        {
            return string.Concat(o);
        }

        /// <summary>
        /// string.Concat(o)
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static string ToTrimText(this object o)
        {
            return string.Concat(o).Trim();
        }

        #endregion

        #region Commom

        /// <summary>
        /// Generic 이상, 이하
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <param name="f"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static bool IsBetween<T>(this T s, T f, T t) where T : IComparable<T>
        {
            //if (typeof(T) == typeof(string))
            //{
            //    throw new TypeAccessException("문자열 타입은 IsBetween을 사용할 수 없습니다.");
            //}
            return s.CompareTo(f) >= 0 && s.CompareTo(t) <= 0;
        }

        /// <summary>
        /// p.Contains(s)
        /// </summary>
        /// <param name="s"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static bool IsIn<T>(this T s, params T[] p)
        {
            if (s == null)
            {
                throw new ArgumentException();
            }
            return p.Contains(s);
        }

        #endregion

    }
}
