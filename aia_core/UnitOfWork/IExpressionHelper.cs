using System.Linq.Expressions;

namespace aia_core.UnitOfWork
{
    public class SwapVisitor : ExpressionVisitor
    {
        private readonly Expression from, to;
        public SwapVisitor(Expression from, Expression to)
        {
            this.from = from;
            this.to = to;
        }
        public override Expression Visit(Expression node)
        {
            return node == from ? to : base.Visit(node);
        }
    }
    public interface IExpressionHelper<T> where T : class
    {
        Expression<Func<T, bool>> Combine(ExpressionType type, Expression<Func<T, bool>> left, Expression<Func<T, bool>> right);
    }
    public class ExpressionHelper<T> : IExpressionHelper<T> where T : class
    {
        public ExpressionHelper() { }
        public Expression<Func<T, bool>> Combine(ExpressionType type, Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        {
            switch (type)
            {
                case ExpressionType.Add:
                    return Expression.Lambda<Func<T, bool>>
                            (Expression.And(new SwapVisitor(left.Parameters[0], right.Parameters[0]).Visit(left.Body), right.Body), right.Parameters);

                case ExpressionType.AndAlso:
                    return Expression.Lambda<Func<T, bool>>
                            (Expression.AndAlso(new SwapVisitor(left.Parameters[0], right.Parameters[0]).Visit(left.Body), right.Body), right.Parameters);

                case ExpressionType.Or:
                    return Expression.Lambda<Func<T, bool>>
                            (Expression.Or(new SwapVisitor(left.Parameters[0], right.Parameters[0]).Visit(left.Body), right.Body), right.Parameters);

                case ExpressionType.And:
                    return Expression.Lambda<Func<T, bool>>
                            (Expression.And(new SwapVisitor(left.Parameters[0], right.Parameters[0]).Visit(left.Body), right.Body), right.Parameters);

                case ExpressionType.GreaterThan:
                    return Expression.Lambda<Func<T, bool>>
                            (Expression.GreaterThan(new SwapVisitor(left.Parameters[0], right.Parameters[0]).Visit(left.Body), right.Body), right.Parameters);

                case ExpressionType.GreaterThanOrEqual:
                    return Expression.Lambda<Func<T, bool>>
                            (Expression.GreaterThanOrEqual(new SwapVisitor(left.Parameters[0], right.Parameters[0]).Visit(left.Body), right.Body), right.Parameters);

                case ExpressionType.LessThan:
                    return Expression.Lambda<Func<T, bool>>
                            (Expression.LessThan(new SwapVisitor(left.Parameters[0], right.Parameters[0]).Visit(left.Body), right.Body), right.Parameters);

                case ExpressionType.LessThanOrEqual:
                    return Expression.Lambda<Func<T, bool>>
                            (Expression.LessThanOrEqual(new SwapVisitor(left.Parameters[0], right.Parameters[0]).Visit(left.Body), right.Body), right.Parameters);

                case ExpressionType.Equal:
                    return Expression.Lambda<Func<T, bool>>
                            (Expression.Equal(new SwapVisitor(left.Parameters[0], right.Parameters[0]).Visit(left.Body), right.Body), right.Parameters);

                default:
                    return null;
            }
        }
    }
}
