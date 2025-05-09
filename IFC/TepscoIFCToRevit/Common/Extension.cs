using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.Common
{
    public static class Extension
    {
        public static bool IsIntersectNotParallel(this Line line1, Line line2)
        {
            if (line1 != null && line2 != null)
                return !RevitUtils.IsParallel(line1.Direction, line2.Direction) &&
                        RevitUtils.IsLineIntersect(line1, line2);

            return false;
        }

        #region conversion

        public static double ToMM(this double feet)
        {
            return feet * 304.8;
        }

        public static double ToFeet(this double mm)
        {
            return mm / 304.8;
        }

        public static XYZ ToMM(this XYZ feet)
        {
            if (feet != null)
            {
                return new XYZ(
                    feet.X.ToMM(),
                    feet.Y.ToMM(),
                    feet.Z.ToMM());
            }
            return null;
        }

        public static XYZ ToFeet(this XYZ mm)
        {
            if (mm != null)
            {
                return new XYZ(
                    mm.X.ToFeet(),
                    mm.Y.ToFeet(),
                    mm.Z.ToFeet());
            }
            return null;
        }

        #endregion conversion

        #region list

        /// <summary>
        /// apply action to each item in the given list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="action"></param>
        public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
        {
            if (list?.Count() > 0 && action != null)
            {
                foreach (T item in list)
                    action.Invoke(item);
            }
        }

        /// <summary>
        /// calculate the sum of all points in list
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static XYZ Sum(this IEnumerable<XYZ> points)
        {
            if (points?.Count() > 0)
            {
                XYZ sum = XYZ.Zero;
                points.ForEach(x => sum += x);
                return sum;
            }
            return null;
        }

        /// <summary>
        /// calucalte the average point from all points in given list
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static XYZ Average(this IEnumerable<XYZ> points)
        {
            if (points?.Count() > 0)
                return points.Sum() / points.Count();
            return null;
        }

        /// <summary>
        /// Order point by given direction vector
        /// </summary>
        /// <param name="points"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static List<XYZ> OrderBy(this List<XYZ> points, XYZ direction)
        {
            if (points?.Count > 0 && direction?.IsZeroLength() == false)
            {
                direction = direction.Normalize();
                return points.OrderBy(x => x.DotProduct(direction)).ToList();
            }
            return new List<XYZ>();
        }

        public static List<T> OrderBy<T>(this IEnumerable<T> list, Func<T, XYZ> selector, XYZ direction)
        {
            if (list?.Count() > 0
                && selector != null
                && direction?.IsZeroLength() == false)
            {
                direction = direction.Normalize();
                return list.OrderBy(x => selector.Invoke(x).DotProduct(direction)).ToList();
            }
            return new List<T>();
        }

        public static bool OrderedPairMatch<T>(this List<T> list_0, List<T> list_1, Func<T, T, double, bool> comparer, double tolerance)
        {
            if (list_0 != null
                && list_1 != null
                && list_0.Count == list_1.Count
                && comparer != null)
            {
                for (int i = 0; i < list_0.Count; i++)
                {
                    if (!comparer(list_0[i], list_1[i], tolerance))
                        return false;
                }
                return true;
            }
            return false;
        }

        public static T Max<T, TCompareValue>(this IEnumerable<T> list, Func<T, TCompareValue> selector, Func<TCompareValue, TCompareValue, bool> isLessThan)
        {
            if (list?.Count() > 0 && selector != null)
            {
                T[] array = list.ToArray();
                T max = array[0];
                TCompareValue maxValue = selector.Invoke(max);

                for (int i = 1; i < array.Length; i++)
                {
                    TCompareValue curValue = selector.Invoke(array[i]);
                    if (isLessThan(maxValue, curValue))
                    {
                        max = array[i];
                        maxValue = selector.Invoke(max);
                    }
                }
                return max;
            }
            return default(T);
        }

        public static XYZ GetBoxMin(this IEnumerable<XYZ> list)
        {
            if (list?.Count() > 0)
            {
                double X = list.Min(x => x.X);
                double Y = list.Min(x => x.Y);
                double Z = list.Min(x => x.Z);

                return new XYZ(X, Y, Z);
            }
            return null;
        }

        public static XYZ GetBoxMax(this IEnumerable<XYZ> list)
        {
            if (list?.Count() > 0)
            {
                double X = list.Max(x => x.X);
                double Y = list.Max(x => x.Y);
                double Z = list.Max(x => x.Z);

                return new XYZ(X, Y, Z);
            }
            return null;
        }

        #endregion list
    }
}