using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BlazorTableDemo.Models
{
    [Flags]
    public enum TableStyle
    {
        Dark = 1,
        Striped = 2,
        Bordered = 4,
        Borderless = 8,
        Hover = 16,
        Sm = 32,
    }
    public enum SortDirection
    {
        Ascending = 1,
        Descending = 2,
        None = 0
    }

    public static class Utils
    {
        /// <summary>
        /// Obtains the property name used on an expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static string GetPropertyName<T>(this Expression<Func<T,object>> expression) =>
            expression.Body switch
            {
                MemberExpression m =>
                    m.Member.Name,
                UnaryExpression u when u.Operand is MemberExpression m =>
                    m.Member.Name,
                MethodCallExpression c when c.Arguments.Count > 0 =>                    
                    ((MemberExpression)c.Arguments[0]).Member.Name,
                MethodCallExpression o when o.Object is MemberExpression m =>
                    m.Member.Name,
                ConditionalExpression i when i.Test is MemberExpression m =>
                                    ((MemberExpression)m.Expression).Member.Name,
                _ =>
                    throw new NotImplementedException(expression.GetType().ToString())
            };

        /// <summary>
        /// Returns an Expression for a given property name on a specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static Expression<Func<T, object>> GetExpression<T>(string propertyName)
        {
            var param = Expression.Parameter(typeof(T), "x");
            // creates a unaryexpression for the property
            Expression conversion = Expression.Convert(Expression.Property(param, propertyName), typeof(object));
            return Expression.Lambda<Func<T, object>>(conversion, param);
        }

        /// <summary>
        /// Creates a delegate for a specific type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static Func<T, object> GetFunc<T>(string propertyName)
        {
            return GetExpression<T>(propertyName).Compile();  // only need compiled expression
        }

        /// <summary>
        /// Overloads the OrderBy method to accept property name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static IOrderedEnumerable<T>OrderBy<T>(this IEnumerable<T> source, string propertyName)
        {
            return source.OrderBy(GetFunc<T>(propertyName));
        }

        /// <summary>
        /// Overloads the OrderBy method to accept property name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static IOrderedQueryable<T>OrderBy<T>(this IQueryable<T> source, string propertyName)
        {
            return source.OrderBy(GetExpression<T>(propertyName));
        }

        /// <summary>
        /// Overloads the OrderByDescending method to accept property name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static IOrderedEnumerable<T> OrderByDescending<T>(this IEnumerable<T> source, string propertyName)
        {
            return source.OrderByDescending(GetFunc<T>(propertyName));
        }

        /// <summary>
        /// Overloads the OrderByDescending method to accept property name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string propertyName)
        {
            return source.OrderByDescending(GetExpression<T>(propertyName));
        }

        /// <summary>
        /// Conditional Lambda expression without breaking the flow of the expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elements"></param>
        /// <param name="condition"></param>
        /// <param name="thenPath"></param>
        /// <param name="elsePath"></param>
        /// <returns></returns>
        public static IEnumerable<T> IfThenElse<T>(
            this IEnumerable<T> elements,
            Func<bool> condition,
            Func<IEnumerable<T>, IEnumerable<T>> thenPath,
            Func<IEnumerable<T>, IEnumerable<T>> elsePath)
        {
            return condition()
                ? thenPath(elements)
                : elsePath(elements);
        }

        /// <summary>
        /// Conditional Lambda expression without breaking the flow of the expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elements"></param>
        /// <param name="condition"></param>
        /// <param name="thenPath"></param>
        /// <param name="elsePath"></param>
        /// <returns></returns>
        public static IQueryable<T> IfThenElse<T>(
            this IQueryable<T> elements,
            Func<bool> condition,
            Func<IQueryable<T>, IQueryable<T>> thenPath,
            Func<IQueryable<T>, IQueryable<T>> elsePath)
        {
            return condition()
                ? thenPath(elements)
                : elsePath(elements);
        }

        /// <summary>
        /// Does creates a BinaryExpression that represents an AndAlso with the two provided expressions.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        {
            var invokedExpr = Expression.Invoke(right, left.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, invokedExpr), left.Parameters);
        }
    }
}
