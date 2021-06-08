using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using TwitchBot.Messages;

namespace TwitchBot
{
    /// <summary>
    /// DS 2021-02-13: The logic helper class to execute an compile conditions
    /// </summary>
    public static class Logic
    {
        #region Expression

        /// <summary>
        /// Executes the condition
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="handler"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool ExecuteCondition(string condition, OnResponseMessageParameterHandler handler, bool defaultValue)
        {
            var expression = GetExpression(condition);
            if (expression != null)
            {
                return expression.Invoke(handler);
            }
            return defaultValue;
        }

        /// <summary>
        /// Compiles the condition
        /// </summary>
        /// <param name="condition"></param>
        public static void CompileCondition(string condition)
        {
            GetExpression(condition);
        }

        /// <summary>
        /// The condition delegate
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        private delegate bool ConditionDelegate(OnResponseMessageParameterHandler handler);

        /// <summary>
        /// The cache of conditions
        /// </summary>
        private static readonly Dictionary<string, ConditionDelegate> ConditionCache = new Dictionary<string, ConditionDelegate>();

        /// <summary>
        /// Returns the expression
        /// </summary>
        /// <param name="conditionText"></param>
        /// <returns></returns>
        private static ConditionDelegate GetExpression(string conditionText)
        {
            // Ignore null
            if (conditionText == null)
                return null;

            // Gets the expression from cache
            if (ConditionCache.TryGetValue(conditionText, out var condition))
                return condition;

            // Compiles the expression
            var expression = BuildExpression(conditionText);
            if (expression != null)
            {
                condition = expression.Compile();
                ConditionCache.Add(conditionText, condition);
                return condition;
            }

            return null;
        }

        /// <summary>
        /// The parameter for the handler
        /// </summary>
        private static readonly ParameterExpression ParameterHandler = Expression.Parameter(typeof(OnResponseMessageParameterHandler), "handler");

        /// <summary>
        /// Builds the expression
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        private static Expression<ConditionDelegate> BuildExpression(string condition)
        {
            // Builds the body
            var p = 0;
            var body = ReadExpression(condition, ref p);

            // Ensure this is a boolean type
            body = ExpressionChangeToBoolean(body);

            // Builds the method
            var lambda = Expression.Lambda<ConditionDelegate>(body, ParameterHandler);
            return lambda;
        }

        /// <summary>
        /// The expression type read by <see cref=""/>
        /// </summary>
        private enum ExpressionType
        {
            None,
            String,
            Boolean,
            Integer,
            Method,
            Operator,
            Parameter,
        }

        /// <summary>
        /// Reads the next element
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private static (ExpressionType, string) ReadNextElement(string condition, ref int p)
        {
            var isString = false;
            var isParameter = false;
            var isOperator = false;
            var builder = new StringBuilder();
            for (; p < condition.Length; p++)
            {
                var c = condition[p];

                // Ignore white spaces
                if (builder.Length == 0 && !isString)
                {
                    if (char.IsWhiteSpace(c))
                        continue;
                }

                // Detect the end of the word
                if (char.IsWhiteSpace(c) && !isString && !isParameter)
                {
                    break;
                }
                // Checks for methods or blocks
                else if (c == '(' && !isString && !isParameter)
                {
                    if (builder.Length > 0)
                    {
                        return (ExpressionType.Method, builder.ToString());
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                // Checks for strings
                else if (c == '\'' && !isParameter)
                {
                    if (isString)
                    {
                        p++;
                        break;
                    }
                    else
                    {
                        if (builder.Length == 0)
                        {
                            isString = true;
                        }
                        else
                        {
                            p--;
                            break;
                        }
                    }
                }
                // Checks for parameter
                else if (c == '{' && !isString && !isParameter)
                {
                    if (builder.Length == 0)
                    {
                        isParameter = true;
                    }
                    else
                    {
                        p--;
                        break;
                    }
                }
                // Checks for parameter
                else if (c == '}' && isParameter)
                {
                    p++;
                    break;
                }
                // Checks for operators
                else if ((c == '=' || c == '!' || c == '>' || c == '<' || c == '|' || c == '&') && !isString && !isParameter)
                {
                    if (isOperator)
                    {
                        builder.Append(c);
                    }
                    else
                    {
                        if (builder.Length == 0)
                        {
                            isOperator = true;
                            builder.Append(c);
                        }
                        else
                        {
                            p--;
                            break;
                        }
                    }
                }
                // Continue with the string
                else
                {
                    // This is an operator, end here
                    if (isOperator)
                    {
                        p--;
                        break;
                    }

                    // Continue with the word
                    builder.Append(c);
                }
            }

            var text = builder.ToString();
            var textLower = text.ToLowerInvariant();

            // String
            if (isString)
            {
                return (ExpressionType.String, text);
            }

            // Parameter
            if (isParameter)
            {
                return (ExpressionType.Parameter, text);
            }

            // Operator
            if (isOperator)
            {
                return (ExpressionType.Operator, text);
            }

            // Boolean
            if (textLower == "true" || textLower == "false")
            {
                return (ExpressionType.Boolean, textLower);
            }

            // Integer
            if (int.TryParse(text, out var _))
            {
                return (ExpressionType.Integer, text);
            }

            // Operators
            if (text == "&&" || text == "||" || text == "==" || text == "!=" || text == ">" || text == "<" || text == ">=" || text == "<=")
            {
                return (ExpressionType.Operator, text);
            }

            // Unknown?
            if (string.IsNullOrEmpty(text))
            {
                return (ExpressionType.None, null);
            }


            // Parameter
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads the next expression
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private static Expression ReadExpression(string condition, ref int p)
        {
            ExpressionType type;
            string name;

            // Reads the next element
            (type, name) = ReadNextElement(condition, ref p);

            Expression left, right;
            switch (type)
            {
                case ExpressionType.Boolean:
                    left = Expression.Constant(name == "true");
                    break;
                case ExpressionType.String:
                    left = Expression.Constant(name);
                    break;
                case ExpressionType.Integer:
                    left = Expression.Constant(int.Parse(name));
                    break;
                case ExpressionType.Parameter:
                    left = Expression.Invoke(ParameterHandler, Expression.Constant(name, typeof(string)), Expression.Constant(null, typeof(string)));
                    break;
                default:
                    throw new NotImplementedException();
            }

            // Reads the next element
            (type, name) = ReadNextElement(condition, ref p);

            switch (type)
            {
                case ExpressionType.None:
                    return left;
                case ExpressionType.Operator:
                    right = ReadExpression(condition, ref p);
                    switch (name)
                    {
                        case "&&":
                            return Expression.Add(ExpressionChangeToBoolean(left), ExpressionChangeToBoolean(right));
                        case "||":
                            return Expression.Or(ExpressionChangeToBoolean(left), ExpressionChangeToBoolean(right));
                        case "==":
                            return Expression.Equal(left, right);
                        case "!=":
                            return Expression.NotEqual(left, right);
                        case ">":
                            return Expression.GreaterThan(ExpressionChangeToInteger(left), ExpressionChangeToInteger(right));
                        case "<":
                            return Expression.LessThan(ExpressionChangeToInteger(left), ExpressionChangeToInteger(right));
                        case ">=":
                            return Expression.GreaterThanOrEqual(ExpressionChangeToInteger(left), ExpressionChangeToInteger(right));
                        case "<=":
                            return Expression.LessThanOrEqual(ExpressionChangeToInteger(left), ExpressionChangeToInteger(right));
                        default:
                            throw new ArgumentException("Unknown operator!");
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Changes the return type of the expression to a boolean
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private static Expression ExpressionChangeToBoolean(Expression expression)
        {
            // Convert from string
            if (expression.Type == typeof(string))
            {
                Func<string, bool> func = ChangeStringToBoolean;
                return Expression.Call(func.Method, expression);
            }
            // Convert from integer
            if (expression.Type == typeof(int))
            {
                Func<int, bool> func = ChangeIntegerToBoolean;
                return Expression.Call(func.Method, expression);
            }
            return expression;
        }

        /// <summary>
        /// Changes the type to a boolean
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool ChangeStringToBoolean(string value)
        {
            if (value == null)
                return false;
            return value.Equals("true", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Changes the type to a boolean
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool ChangeIntegerToBoolean(int value)
        {
            return value > 0;
        }

        /// <summary>
        /// Changes the return type of the expression to an integer
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private static Expression ExpressionChangeToInteger(Expression expression)
        {
            // Convert from string
            if (expression.Type == typeof(string))
            {
                Func<string, int> func = ChangeStringToInteger;
                return Expression.Call(func.Method, expression);
            }
            return expression;
        }

        /// <summary>
        /// Changes the type to an integer
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static int ChangeStringToInteger(string value)
        {
            if (value == null)
                return 0;
            if (int.TryParse(value, out var integer))
                return integer;
            return 0;
        }

        #endregion Expression
    }
}
