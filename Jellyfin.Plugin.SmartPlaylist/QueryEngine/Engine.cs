using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Diagnostics;

namespace Jellyfin.Plugin.SmartPlaylist.QueryEngine
{
    public static class StringEx
    {
        public static bool MatchRegex(this string regexStr, string input, bool ignoreCase)
        {
            var regex = new Regex(regexStr, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            return regex.IsMatch(input);
        }
    }

    // This is taken entirely from https://stackoverflow.com/questions/6488034/how-to-implement-a-rule-engine
    public class Engine
    {
        static System.Linq.Expressions.Expression BuildExpr<T>(Expression r, ParameterExpression param)
        {
            try
            {
                System.Linq.Expressions.Expression left = System.Linq.Expressions.Expression.Property(param, r.MemberName);
                var leftType = typeof(T).GetProperty(r.MemberName).PropertyType;

                var op = ParseOperator(r.Operator, out var invert);
                invert = invert || r.Invert;

                var rightVal = r.TargetValue;
                if (r.IgnoreCase)
                    rightVal = rightVal.ToLower();

                if (r.IgnoreCase && leftType.Name == "String")
                {
                    left = ToLowerHelper(left);
                }

                System.Linq.Expressions.Expression call;

                // is the operator a known .NET operator?
                if (Enum.TryParse(op, out ExpressionType tBinary))
                {
                    // use a binary operation, e.g. 'Equal' -> 'u.Age == 15'
                    var right = System.Linq.Expressions.Expression.Constant(Convert.ChangeType(rightVal, leftType));
                    call = System.Linq.Expressions.Expression.MakeBinary(tBinary, left, right);

                    if (invert)
                        call = System.Linq.Expressions.Expression.Not(call);

                    return call;
                }

                // Custom Methods
                var method = typeof(StringEx).GetMethod(op);
                if (method != null)
                {
                    var right = System.Linq.Expressions.Expression.Constant(rightVal);
                    var ignoreCase = System.Linq.Expressions.Expression.Constant(r.IgnoreCase);
                    call = MethodHelper(r.IgnoreCase, method, left, right, ignoreCase);

                    if (invert) call = System.Linq.Expressions.Expression.Not(call);
                    return call;
                }

                var method2 = leftType.GetMethod(op, new[] { typeof(string) });
                if (method2 != null)
                {
                    var right = System.Linq.Expressions.Expression.Constant(rightVal);
                    call = MethodHelper(r.IgnoreCase, left, method2, right);

                    if (invert) call = System.Linq.Expressions.Expression.Not(call);
                    return call;
                }
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Debugger.Break();
            }

            return null;
        }

        private static System.Linq.Expressions.Expression MethodHelper(bool ignoreCase, MethodInfo method, params System.Linq.Expressions.Expression[] exp)
        {
            return MethodHelper(ignoreCase, null, method, exp);
        }

        private static System.Linq.Expressions.Expression MethodHelper(bool ignoreCase, System.Linq.Expressions.Expression? expression, MethodInfo method, params System.Linq.Expressions.Expression[] exp)
        {
            var changeTypeMethod = typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) });

            var c = 0;
            foreach (var param in method.GetParameters())
            {
                var tParam = param.ParameterType;
                var tParamExp = System.Linq.Expressions.Expression.Constant(tParam);

                var result = Convert.ChangeType("test", typeof(string));

                if (changeTypeMethod != null && exp[c].Type != tParam)
                {
                    var left = System.Linq.Expressions.Expression.Convert(exp[c], typeof(object));
                    exp[c] = System.Linq.Expressions.Expression.Call(null, changeTypeMethod, left, tParamExp);
                    exp[c] = System.Linq.Expressions.Expression.Convert(exp[c], tParam);
                }

                if (tParam.Name == "String" && ignoreCase)
                    exp[c] = ToLowerHelper(exp[c]);
                c++;
            }

            System.Linq.Expressions.Expression call = System.Linq.Expressions.Expression.Call(expression, method, exp);
            if (expression != null)
            {
                var nullCheck = System.Linq.Expressions.Expression.NotEqual(expression, System.Linq.Expressions.Expression.Constant(null, typeof(object)));
                return System.Linq.Expressions.Expression.AndAlso(nullCheck, call);
            }
            else
                return call;
        }

        private static System.Linq.Expressions.Expression ToLowerHelper(System.Linq.Expressions.Expression exp)
        {
            return System.Linq.Expressions.Expression.Condition(
                         System.Linq.Expressions.Expression.Equal(exp, System.Linq.Expressions.Expression.Constant(null)),
                         System.Linq.Expressions.Expression.Constant(null, exp.Type),
                         System.Linq.Expressions.Expression.Call(exp, "ToLower", null));
        }


        private static string ParseOperator(string @operator, out bool inverted)
        {
            if (@operator.ToLower().StartsWith("not"))
            {
                @operator = @operator.Substring(3).TrimStart();
                inverted = true;
                return @operator;
            }
            if (@operator[0] == '!')
            {
                @operator = @operator.Substring(1);
                inverted = true;
                return @operator;
            }
            inverted = false;
            return @operator;
        }

        public static Func<T, bool> CompileRule<T>(Expression r)
        {
            var paramUser = System.Linq.Expressions.Expression.Parameter(typeof(T));
            System.Linq.Expressions.Expression expr = BuildExpr<T>(r, paramUser);
            // build a lambda function User->bool and compile it

            if (expr != null)
            {
                var value = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(expr, paramUser).Compile(true);
                return value;
            }
            return null;
        }

        public static List<ExpressionSet> FixRuleSets(List<ExpressionSet> rulesets)
        {
            foreach (var rules in rulesets)
            {
                FixRules(rules);
            }
            return rulesets;
        }

        public static ExpressionSet FixRules(ExpressionSet rules)
        {
            foreach (var rule in rules.Expressions)
            {
                if (rule.MemberName == "PremiereDate")
                {
                    var somedate = DateTime.Parse(rule.TargetValue);
                    rule.TargetValue = ConvertToUnixTimestamp(somedate).ToString();
                }
            }
            return rules;
        }

        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }
    }
}
