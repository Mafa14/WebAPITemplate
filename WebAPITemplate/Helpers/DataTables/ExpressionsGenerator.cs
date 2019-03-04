using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using WebAPITemplate.Database.Models;
using WebAPITemplate.RequestContracts.DataTable;

namespace WebAPITemplate.Helpers.DataTables
{
    public static class ExpressionsGenerator
    {
        private const string AscendingNameConvention = "ASC";

        public static Expression<Func<Users, bool>> GetFilter<T>(Column[] columns, string prefix)
        {
            ParameterExpression argParam = Expression.Parameter(typeof(T), prefix);
            Expression currentExpression = null;

            foreach (var column in columns)
            {
                currentExpression = AssembleExpression<T>(argParam, currentExpression, column);
            }

            if (currentExpression == null)
            {
                return null;
            }

            return Expression.Lambda<Func<Users, bool>>(currentExpression, argParam);
        }

        public static IOrderedQueryable<T> OrderFilter<T>(IQueryable<T> query, Column[] columns, Order[] orders)
        {
            bool firstOrder = true;
            IOrderedQueryable<T> sortedQuery = null;

            foreach (var order in orders)
            {
                Column currentColumn = columns[order.Column];
                PropertyInfo propertyInfo = typeof(T).GetProperty(currentColumn.Name);

                if (firstOrder)
                {
                    sortedQuery = order.Dir.ToUpper() == AscendingNameConvention ?
                        query.OrderBy(x => propertyInfo.GetValue(x, null)) :
                        query.OrderByDescending(x => propertyInfo.GetValue(x, null));
                }
                else
                {
                    sortedQuery = order.Dir.ToUpper() == AscendingNameConvention ?
                        sortedQuery.ThenBy(x => propertyInfo.GetValue(x, null)) :
                        sortedQuery.ThenByDescending(x => propertyInfo.GetValue(x, null));
                }
            }

            return sortedQuery;
        }

        private static Expression AssembleExpression<T>(ParameterExpression argParam, Expression currentExpression, Column currentColumn)
        {
            Expression expression = null;
            if (currentColumn.Search.Value != string.Empty)
            {
                PropertyInfo propertyInfo = typeof(T).GetProperty(currentColumn.Name);

                if (propertyInfo.PropertyType == typeof(string))
                {
                    MethodInfo method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    expression = Expression.Call(Expression.Property(argParam, currentColumn.Name), method, Expression.Constant(currentColumn.Search.Value));
                }
                else
                {
                    expression = Expression.Equal(Expression.Property(argParam, currentColumn.Name), Expression.Constant(currentColumn.Search.Value));
                }
            }

            if (currentExpression == null && expression != null)
            {
                currentExpression = expression;
            }
            else if (currentExpression != null && expression != null)
            {
                currentExpression = Expression.AndAlso(currentExpression, expression);
            }

            return currentExpression;
        }
    }
}
